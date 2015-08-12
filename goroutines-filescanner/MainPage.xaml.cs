using System;
using System.Collections.Generic;
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
        public MainPage()
        {
            this.InitializeComponent();
            textBox.Text = "";
        }

        private async void selectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (await EnsureUnsnapped()) {
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
        }

        internal async Task<bool> EnsureUnsnapped()
        {
            // FilePicker APIs will not work if the application is in a snapped state.
            // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped) {
                var dlg = new MessageDialog("Cannot unsnap to show a folder picker");
                await dlg.ShowAsync();
            }

            return unsnapped;
        }

        struct FileInfo
        {
            public string Name;
            public string Path;
            public UInt64 Size;
            public string Sha1; 
        }

        private async void go_Click(object sender, RoutedEventArgs e)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(textBox.Text);
            var results = new Channel<FileInfo>();

            Go.Run(scanDir, folder, results);

            await results.ForEach(fi => {
                var gi = new GridViewItem();
                gi.Content = fi.Name;
                grid.Items.Add(gi);
            });
        }

        private async Task scanDir(StorageFolder folder, Channel<FileInfo> results)
        {
            var sha1 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

            foreach(var fi in await folder.GetItemsAsync()) {
                var sf = fi as StorageFile;
                if (sf == null)
                    continue;
                
                var basicProps = await sf.GetBasicPropertiesAsync();
                IBuffer buffer;
                using (var stream = await sf.OpenAsync(FileAccessMode.Read)) {
                    buffer = WindowsRuntimeBuffer.Create((int)basicProps.Size); // oh no we can't read large files

                    await stream.ReadAsync(buffer, (uint)basicProps.Size, InputStreamOptions.None);
                }
                    
                var hash = sha1.HashData(buffer);
                var hashBytes = new byte[hash.Length];
                hash.CopyTo(hashBytes);

                await results.Send(new FileInfo { Name = fi.Name, Sha1 = Convert.ToBase64String(hashBytes) });
            }
            results.Close();
        }
    }
}
