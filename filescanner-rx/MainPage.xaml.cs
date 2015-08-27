using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace filescanner_rx
{
    public sealed partial class MainPage : Page
    {
        class FileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public UInt64 Size { get; set; }
            public string Sha1 { get; set; }
        }

        readonly Subject<string> m_directoryChanged = new Subject<string>();
        ObservableCollection<FileInfo> m_observableCollection = new ObservableCollection<FileInfo>();

        public MainPage()
        {
            this.InitializeComponent();
            textBox.Text = "";
            listView.ItemsSource = m_observableCollection;

            StartListeningOnChannels();
        }

        // Rx await with FirstAsync doesn't work properly
        //
        // FirstAsync waits and listens for the next value, then it stops listening. 
        // If a value arrives while we're doing something else, we miss the value. We'd need to implement some kind of
        // buffering mechanism to prevent this.
        //
        // If we did buffer, then how would you control the buffer size? do you block the call to OnNext? 
        // If we did that then it would block the OS thread as OnNext is synchronous which would kill perf/scalability
        // by the time we solve both those problems we've basically implemented channels.
        //
        // What could be quite interesting though is an Observable-to-channel adapter, as (OnError notwithstanding)
        // channels are a superset observables, so this should work
        async void StartListeningOnChannels()
        {
            while (true) { // how do we check for onCompleted
                string directoryName;
                try {
                    directoryName = await m_directoryChanged.FirstAsync();
                }
                catch (InvalidOperationException) {
                    break; // no more
                }
                var folder = await StorageFolder.GetFolderFromPathAsync(directoryName);

                var scanStarts = new Subject<string>();
                var scanCompletes = new Subject<FileInfo>();
                var _ = Task.Run(() => ScanDirectoryAsync(folder, true, scanStarts, scanCompletes));

                m_observableCollection.Clear();
                while (true) {
                    FileInfo fi;
                    try {
                        fi = new FileInfo { Name = await scanStarts.FirstAsync() };
                    }
                    catch (InvalidOperationException) {
                        break; // no more elements
                    }

                    var insertedIndex = m_observableCollection.Count;
                    m_observableCollection.Add(fi);

                    try {
                        fi = await scanCompletes.FirstAsync();
                    }
                    catch (InvalidOperationException) {
                        break; // no more elements
                    }

                    m_observableCollection[insertedIndex] = fi;
                }
            }
        }

        private async Task ScanDirectoryAsync(StorageFolder storageFolder, bool doSha1, IObserver<string> starts, IObserver<FileInfo> completes)
        {
            foreach (var entry in await storageFolder.GetItemsAsync()) {
                var folder = entry as StorageFolder; // folder
                if (folder != null) {
                    starts.OnNext(folder.Name);

                    var innerStarts = new Subject<string>();
                    var innerCompletes = new Subject<FileInfo>();
                    var _ = Task.Run(() => ScanDirectoryAsync(folder, false, innerStarts, innerCompletes));

                    var totalSize = await innerStarts
                        .Zip(innerCompletes, (s, f) => (double)f.Size)
                        .Sum(x => x).FirstAsync();

                    completes.OnNext(new FileInfo { Name = folder.Name, Size = (ulong)totalSize });
                }

                var file = entry as StorageFile; // file
                if (file != null) {
                    starts.OnNext(file.Name);
                    var fileInfo = await ScanFile(file, doSha1).ConfigureAwait(false);
                    completes.OnNext(fileInfo);
                }
            }
            starts.OnCompleted();
            completes.OnCompleted();
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
            m_directoryChanged.OnNext(textBox.Text);
        }

        private static readonly HashAlgorithmProvider Sha1 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

        private async Task<FileInfo> ScanFile(StorageFile file, bool doSha1)
        {
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
