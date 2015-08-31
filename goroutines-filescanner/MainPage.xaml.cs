// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace goroutines_filescanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        class FileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public UInt64 Size { get; set; }
            public string Sha1 { get; set; }
        }

        readonly Channel<string> m_directoryChanged = new Channel<string>();
        ObservableCollection<FileInfo> m_observableCollection = new ObservableCollection<FileInfo>();

        public MainPage()
        {
            this.InitializeComponent();
            textBox.Text = "";
            listView.ItemsSource = m_observableCollection;
            
            StartListeningOnChannels();
        }

        const int BufferSize = 5;
        
        async void StartListeningOnChannels()
        {
            while (m_directoryChanged.IsOpen) {
                var directoryName = await m_directoryChanged.Receive();
                var folder = await StorageFolder.GetFolderFromPathAsync(directoryName);

                var scanStarts = new Channel<string>();
                var scanCompletes = new Channel<FileInfo>();
                Go.Run(ScanDirectoryAsync, folder, true, scanStarts, scanCompletes);

                m_observableCollection.Clear();
                while (true) {
                    var rv = await scanStarts.ReceiveEx();
                    if (!rv.IsValid)
                        break;  // channel closed

                    var fi = new FileInfo { Name = rv.Value };

                    var insertedIndex = m_observableCollection.Count;
                    m_observableCollection.Add(fi);

                    var rv2 = await scanCompletes.ReceiveEx();

                    if (!rv2.IsValid)
                        break; // channel closed
                    m_observableCollection[insertedIndex] = rv2.Value;
                }
            }
        }
        
        private async Task ScanDirectoryAsync(StorageFolder storageFolder, bool doSha1, Channel<string> starts, Channel<FileInfo> completes)
        {
            using (starts)
            using (completes) {
                foreach (var entry in await storageFolder.GetItemsAsync()) {
                    var folder = entry as StorageFolder; // folder
                    if (folder != null) {
                        await starts.Send(folder.Name);

                        // some small amount of buffering should improve perf as we don't have to task-switch so often
                        var innerStarts = new Channel<string>();
                        var innerCompletes = new Channel<FileInfo>();
                        Go.Run(ScanDirectoryAsync, folder, false, innerStarts, innerCompletes);

                        var totalSize = await innerStarts
                            .Zip(innerCompletes, (s, f) => f.Size)
                            .Sum(x => x).ConfigureAwait(false);
                        
                        await completes.Send(new FileInfo { Name = folder.Name, Size = totalSize });
                    }

                    var file = entry as StorageFile; // file
                    if (file != null) {
                        var fi = new FileInfo{ Name = file.Name };
                        await starts.Send(file.Name);
                        try {
                            fi = await ScanFile(file, doSha1).ConfigureAwait(false);
                        } catch(Exception e) {
                            Debug.WriteLine($"{e} scanning {file.Name}");
                        }
                        await completes.Send(fi); // if we fail, publish an empty entry to complete the sequence
                    }
                }
            }
        }

        private async void selectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            folderPicker.FileTypeFilter.Add(".NoSuchExtension");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null) {
                // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                textBox.Text = folder.Path;
            }
            await m_directoryChanged.Send(textBox.Text);
        }

        private static readonly HashAlgorithmProvider Sha1 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

        private async Task<FileInfo> ScanFile(StorageFile file, bool doSha1)
        {
            var fn = file.Name;
            var basicProps = await file.GetBasicPropertiesAsync();

            string sha1str = null;
            if (doSha1) {
                try {
                    IBuffer buffer;
                    using (var stream = await file.OpenAsync(FileAccessMode.Read)) {
                        buffer = WindowsRuntimeBuffer.Create((int)basicProps.Size); // oh no we can't read large files
                        await stream.ReadAsync(buffer, (uint)basicProps.Size, InputStreamOptions.None);
                    }

                    var hash = Sha1.HashData(buffer);
                    var hashBytes = new byte[hash.Length];
                    hash.CopyTo(hashBytes);
                    sha1str = Convert.ToBase64String(hashBytes);
                }
                catch (UnauthorizedAccessException) { }
            }

            return new FileInfo {
                Name = file.Name,
                Path = file.Path,
                Sha1 = sha1str,
                Size = basicProps.Size,
            };
        }
    }
}
