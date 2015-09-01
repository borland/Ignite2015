// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace goroutines_filescanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        readonly Channel<string> m_directoryChanged = new Channel<string>();
        ObservableCollection<FileInfo> m_observableCollection = new ObservableCollection<FileInfo>();

        public static Color BackgroundColor = Color.FromArgb(255, 45, 187, 40);

        public MainPage()
        {
            this.InitializeComponent();
            textBox.Text = "";
            listView.ItemsSource = m_observableCollection;

            canvas.SizeChanged += (s, e) => { var _ = DrawTreeMap(); };

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = titleBar.ButtonBackgroundColor = titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor = BackgroundColor;
            relativePanel.Background = new SolidColorBrush(BackgroundColor);

            StartListeningOnChannels();
        }

        async void StartListeningOnChannels()
        {
            while (m_directoryChanged.IsOpen) {
                var directoryName = await m_directoryChanged.Receive();
                StorageFolder folder;
                try {
                    folder = await StorageFolder.GetFolderFromPathAsync(directoryName);
                }
                catch (Exception) {
                    continue; // invalid folder
                }

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
                    fi.CopyFrom(rv2.Value);

                    await DrawTreeMap();
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

                        var innerStarts = new Channel<string>();
                        var innerCompletes = new Channel<FileInfo>();
                        Go.Run(ScanDirectoryAsync, folder, false, innerStarts, innerCompletes);

                        var totalSize = await innerStarts
                            .Zip(innerCompletes, (s, f) => f.Size)
                            .Sum(x => x).ConfigureAwait(false);

                        await completes.Send(new FileInfo { Name = folder.Name, Size = totalSize, IsLoaded = true });
                    }

                    var file = entry as StorageFile; // file
                    if (file != null) {
                        var fi = new FileInfo { Name = file.Name };
                        await starts.Send(file.Name);
                        try {
                            fi = await ScanFile(file, doSha1).ConfigureAwait(false);
                        }
                        catch (Exception e) {
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
                IsLoaded = true
            };
        }

        private async Task DrawTreeMap()
        {
            double Width = canvas.ActualWidth;
            double Height = canvas.ActualHeight;
            const double MinSliceRatio = 0.35;

            var collectionCopy = m_observableCollection.ToArray();

            var rectangles = await Task.Run(() => {
                var elements = collectionCopy
                                .Where(x => x.Size > 0) // treemap goes into infinite recursion for 0-sized items
                                 .Select(x => new TreeMap.Element<string> { Object = x.Name, Value = x.Size })
                                 .OrderByDescending(x => x.Value)
                                 .ToList();

                var slice = TreeMap.GetSlice(elements, 1, MinSliceRatio);
                if (slice == null)
                    return Enumerable.Empty<TreeMap.SliceRectangle<string>>();

                return TreeMap.GetRectangles(slice, Width, Height).ToList();
            });

            var white = new SolidColorBrush(Colors.White);
            var grad = new LinearGradientBrush(new GradientStopCollection {
                new GradientStop { Color = Colors.LightBlue, Offset = 0 },
                new GradientStop { Color = Colors.DarkBlue, Offset = 1 },
            }, 47);
            canvas.Children.Clear();

            foreach (var r in rectangles) {
                var rect = new Rectangle { Width = r.Width, Height = r.Height };
                Canvas.SetLeft(rect, r.X);
                Canvas.SetTop(rect, r.Y);
                rect.Fill = grad;

                var text = new TextBlock { Text = r.Slice.Elements.First().Object, Foreground = white };
                Canvas.SetLeft(text, r.X);
                Canvas.SetTop(text, r.Y);

                canvas.Children.Add(rect);
                canvas.Children.Add(text);
            }
        }

        class FileInfo : INotifyPropertyChanged
        {
            string m_name, m_path, m_sha1;
            bool m_isLoaded;
            ulong m_size;

            public event PropertyChangedEventHandler PropertyChanged;

            public void CopyFrom(FileInfo other)
            {
                Name = other.Name;
                Path = other.Path;
                Size = other.Size;
                Sha1 = other.Sha1;
                IsLoaded = other.IsLoaded;
            }

            public string Name
            {
                get { return m_name; }
                set { SetIfDifferent(ref m_name, value); }
            }

            public string Path
            {
                get { return m_path; }
                set { SetIfDifferent(ref m_path, value); }
            }

            public ulong Size
            {
                get { return m_size; }
                set { SetIfDifferent(ref m_size, value); }
            }

            public string Sha1
            {
                get { return m_sha1; }
                set { SetIfDifferent(ref m_sha1, value); }
            }

            public bool IsLoaded
            {
                get { return m_isLoaded; }
                set { SetIfDifferent(ref m_isLoaded, value); }
            }

            void SetIfDifferent<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (Object.Equals(field, value))
                    return;

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var _ = m_directoryChanged.Send(textBox.Text);
        }
    }

    public class VisibleIf : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (Object.Equals(parameter, value.ToString())) // xaml ConverterParameters come in as strings
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        { throw new NotImplementedException(); }
    }
}
