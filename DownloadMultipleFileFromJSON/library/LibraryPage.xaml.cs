using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DownloadMultipleFileFromJSON.library
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryPage : Page
    {
        private DownloadOperation _activeDownload;
        private string[] bukudownloading;

        public LibraryPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPageLoaded;
            this.getContent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //GC.Collect();
            BukuAudio dlList = e.Parameter as BukuAudio;
            if (dlList != null)
            {
                //DownloadBuku(dlList.BundlePath);
                //downloadfilename.Text = dlList.BundleName;
                //Uri uri = new Uri(dlList.BundlePath);
                //string filename = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                //downloadfilename.Text = String.Format("Unduh '{0}'", filename);
                foreach (var path in dlList.BundlePath)
                {
                    DownloadBuku(path);
                    int i = 0;
                    downloadfilename.Text = dlList.BundleName.ElementAt(i);
                    i++;
                    Uri uri = new Uri(path);
                    string filename = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                    downloadfilename.Text = String.Format("Unduh '{0}'", filename);
                }
                DownloadProgress.Visibility = Visibility.Visible;
                downloadfilename.Visibility = Visibility.Visible;
                statusdownload.Visibility = Visibility.Visible;
            }
            else
            {
                DownloadProgress.Visibility = Visibility.Collapsed;
                downloadfilename.Visibility = Visibility.Collapsed;
                statusdownload.Visibility = Visibility.Collapsed;
            }
        }

        private async void MainPageLoaded(object sender, RoutedEventArgs e)
        {
            await LoadActiveDownloadsAsync();
        }

        private StorageFolder installedLocation = ApplicationData.Current.LocalFolder;

        private async Task getContent(String DownloadFileName = "test.pdf")
        {
            StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
            ObservableCollection<BookAudio> datasource = new ObservableCollection<BookAudio>();
            IReadOnlyList<StorageFile> files = await library.GetFilesAsync();
            IEnumerable<Temp> sortingFiles = files.Select(x => new Temp { File = x }).ToList();
            foreach (var item in sortingFiles)
            {
                item.LastModified = (await item.File.GetBasicPropertiesAsync()).DateModified.DateTime;
            }
            IEnumerable<StorageFile> sortedfiles = sortingFiles.OrderByDescending(x => x.LastModified).Select(x => x.File).ToList();
            StorageFolder thumbfolder = await installedLocation.CreateFolderAsync("thumb", CreationCollisionOption.OpenIfExists);

            IReadOnlyList<DownloadOperation> downloads = null;
            downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();

            int i = 0;

            if (downloads.Count > 0)
            {
                bukudownloading = new string[downloads.Count];
                foreach (DownloadOperation download in downloads)
                {
                    // list download buku
                    bukudownloading[i] = download.ResultFile.Name;
                    i++;
                }
            }

            foreach (StorageFile file in sortedfiles)
            {
                BookAudio ba = new BookAudio();
                ba.Name = file.DisplayName.ToString();

                if ((isbukudownloading(file.Name.ToString())) && (file.Name.ToString() != DownloadFileName))
                {
                }
                else
                {
                    //await RenderCoverBuku(file.Name.ToString(), 0);
                    StorageFile thumbFile;
                    bool bookada = true;
                    try
                    {
                        thumbFile = await thumbfolder.GetFileAsync(file.Name.ToString() + ".png");
                        BitmapImage bi = new BitmapImage();
                        bi.SetSource(await thumbFile.OpenAsync(FileAccessMode.Read));
                        ba.Image = bi;
                        datasource.Add(ba);
                    }
                    catch
                    {
                        bookada = false;
                    }
                    if (!bookada)
                    {
                        //await RenderCoverBuku(file.Name.ToString(), 0);
                        var task = Task.Run(async () => { await RenderCoverBuku(file.Name.ToString(), 0); });
                        task.Wait();
                        thumbFile = await thumbfolder.GetFileAsync(file.Name.ToString() + ".png");
                        BitmapImage bi = new BitmapImage();
                        bi.SetSource(await thumbFile.OpenAsync(FileAccessMode.Read));
                        ba.Image = bi;
                        datasource.Add(ba);
                    }
                }
            }

            //this.carousel.ItemsSource = datasource;
            //if (datasource.Count > 0)
            //{
            //    this.carousel.SelectedItem = carousel.Items[0];
            //}
        }

        private PdfDocument _pdfDocument;

        private async Task RenderCoverBuku(string pdfFileName, uint PDF_PAGE_INDEX)
        {
            try
            {
                StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);

                StorageFile pdfFile = await library.GetFileAsync(pdfFileName);
                Boolean free = true;
                //Load Pdf File

                try
                {
                    _pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile, "EastJava");
                }
                catch
                {
                    free = false;
                }

                if (free == false)
                {
                    _pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile, "East#Java101.");
                }

                if (_pdfDocument != null && _pdfDocument.PageCount > 0)
                {
                    //Get Pdf page
                    var pdfPage = _pdfDocument.GetPage(PDF_PAGE_INDEX);

                    if (pdfPage != null)
                    {
                        // next, generate a bitmap of the page
                        StorageFolder thumbfolder = await installedLocation.CreateFolderAsync("thumb", CreationCollisionOption.OpenIfExists);

                        StorageFile jpgFile = await thumbfolder.CreateFileAsync(pdfFileName + ".png", CreationCollisionOption.ReplaceExisting);

                        if (jpgFile != null)
                        {
                            IRandomAccessStream randomStream = await jpgFile.OpenAsync(FileAccessMode.ReadWrite);

                            await pdfPage.RenderToStreamAsync(randomStream);
                            await randomStream.FlushAsync();

                            randomStream.Dispose();
                            pdfPage.Dispose();
                        }
                    }
                }
            }
            catch (Exception err)
            {
            }
        }

        private bool isbukudownloading(string buku)
        {
            bool hasil = false;
            int i = 0;
            if (!(bukudownloading == null))
            {
                foreach (string file in bukudownloading)
                {
                    if (file == buku)
                    {
                        hasil = true;
                    }
                    i++;
                }
            }
            return hasil;
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private async void DownloadClick(object sender, RoutedEventArgs e)
        {
            const string fileLocation
             = "http://bse.mahoni.com/data/Kurikulum%202013/Kelas_01_SD_Tematik_1_Diriku_Siswa.pdf";
            var uri = new Uri(fileLocation);
            var downloader = new BackgroundDownloader();
            StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
            StorageFile file = await library.CreateFileAsync("Kelas_01_SD_Tematik_1_Diriku_Siswa.pdf",
                CreationCollisionOption.ReplaceExisting);
            DownloadOperation download = downloader.CreateDownload(uri, file);
            await StartDownloadAsync(download);
        }

        private async void DownloadBuku(string fileLocation)
        {
            var uri = new Uri(fileLocation);
            var downloader = new BackgroundDownloader();
            StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);
            StorageFile file = await library.CreateFileAsync(filename,
                CreationCollisionOption.ReplaceExisting);
            DownloadOperation download = downloader.CreateDownload(uri, file);
            await StartDownloadAsync(download);
        }

        async private void ProgressCallback(DownloadOperation obj)
        {
            double progress
              = ((double)obj.Progress.BytesReceived / obj.Progress.TotalBytesToReceive);
            progress = Math.Round((double)progress, 2);
            DownloadProgress.Value = progress * 100;
            downloadfilename.Text = String.Format("Downloading '{0}'", obj.ResultFile.Name);
            statusdownload.Text = String.Format("{0} MB/{1} MB ({2}%)", obj.Progress.BytesReceived / 1048576, obj.Progress.TotalBytesToReceive / 1048576, progress * 100);
            DownloadProgress.Visibility = Visibility.Visible;
            downloadfilename.Visibility = Visibility.Visible;
            statusdownload.Visibility = Visibility.Visible;
            if (progress >= 1.0)
            {
                _activeDownload = null;

                DownloadProgress.Value = 0;
                downloadfilename.Visibility = Visibility.Collapsed;
                DownloadProgress.Visibility = Visibility.Collapsed;
                statusdownload.Visibility = Visibility.Collapsed;
                await this.getContent(obj.ResultFile.Name);
            }
        }

        private async Task StartDownloadAsync(DownloadOperation downloadOperation)
        {
            // backButton_Copy.IsEnabled = false;
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            if (connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                _activeDownload = downloadOperation;
                var progress = new Progress<DownloadOperation>(ProgressCallback);
                await downloadOperation.StartAsync().AsTask(progress);
            }
            else
            {
                ConnectionException();
            }
        }

        private async void ConnectionException()
        {
            MessageDialog messageDialog;
            messageDialog = new MessageDialog("Silahkan periksa kembali koneksi internet Anda!", "Gangguan Koneksi Internet");
            messageDialog.Commands.Add(new UICommand("Tutup", (command) =>
            {
                this.Frame.Navigate(typeof(MainPage));
            }));

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        private async Task LoadActiveDownloadsAsync()
        {
            IReadOnlyList<DownloadOperation> downloads = null;
            downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            if (downloads.Count > 0)
            {
                foreach (DownloadOperation download in downloads)
                {
                    // Resume all download
                    await ResumeDownloadAsync(download);
                }
            }
        }

        private async Task ResumeDownloadAsync(DownloadOperation downloadOperation)
        {
            //backButton_Copy.IsEnabled = false;
            _activeDownload = downloadOperation;
            Uri uri = downloadOperation.RequestedUri;
            string filename = System.IO.Path.GetFileName(uri.LocalPath);
            downloadfilename.Text = String.Format("Downloading '{0}'", filename);//downloadOperation.ResultFile.Name);
            var progress = new Progress<DownloadOperation>(ProgressCallback);
            await downloadOperation.AttachAsync().AsTask(progress);
        }

        async private void backButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
            String ResourceReference = "ms-appx:///library/Books/iOS_Succinctly.pdf";
            StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(ResourceReference, UriKind.Absolute));
            await f.CopyAsync(library, f.Name);
            await this.getContent();
        }

        async private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            //if (carousel.Items.Count > 0)
            //{
            //    StorageFolder library = await installedLocation.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
            //    StorageFolder thumb = await installedLocation.CreateFolderAsync("thumb", CreationCollisionOption.OpenIfExists);
            //    BookAudio hapusmajalah = this.carousel.SelectedItem as BookAudio;
            //    MessageDialog messageDialog;
            //    //if (Name == Name + ".mp3")
            //    //{
            //    String deskripsi = String.Format("Yakin menghapus buku/audio '{0}'?", hapusmajalah.Name);
            //    messageDialog = new MessageDialog(deskripsi, "Hapus Audio");

            //    messageDialog.Commands.Add(new UICommand("Hapus", async (command) =>
            //    {
            //        try
            //        {
            //            StorageFile filepdf = await library.GetFileAsync(hapusmajalah.Name + ".pdf");
            //            StorageFile filemp3 = await library.GetFileAsync(hapusmajalah.Name + ".mp3");
            //            StorageFile file1 = await thumb.GetFileAsync(hapusmajalah.Name + ".pdf.png");
            //            StorageFile file = await thumb.GetFileAsync(hapusmajalah.Name + ".mp3.png");
            //            if (hapusmajalah.Name == hapusmajalah.Name + ".pdf.png" && hapusmajalah.Name == hapusmajalah.Name + ".pdf")
            //            {
            //                await filepdf.DeleteAsync();
            //                await file1.DeleteAsync();
            //            }
            //            else if (hapusmajalah.Name == hapusmajalah.Name + ".mp3")
            //            {
            //                await filemp3.DeleteAsync();
            //                await file.DeleteAsync();
            //            }

            //            //if (carousel.Items.Count > 1)
            //            //{
            //            this.carousel.SelectedItem = carousel.Items[0];
            //            await this.getContent();
            //            //}
            //        }
            //        catch
            //        {
            //            this.carousel.SelectedItem = carousel.Items[0];
            //            this.getContent();
            //        }
            //    }));

            //    messageDialog.Commands.Add(new UICommand("Batal", (command) =>
            //    {
            //        //rootPage.NotifyUser("The 'Don't install' command has been selected.", NotifyType.StatusMessage);
            //    }));
            //    // Show the message dialog
            //    await messageDialog.ShowAsync();
            //}
            //else
            //{
            //    HapusBukuException();
            //}
        }

        private async void HapusBukuException()
        {
            MessageDialog messageDialog;
            messageDialog = new MessageDialog("Tidak ada buku/audio yang dapat dihapus", "Buku/Audio Kosong");
            messageDialog.Commands.Add(new UICommand("Tutup", (command) =>
            {
            }));
            await messageDialog.ShowAsync();
        }

        private void carousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        public class Temp
        {
            public StorageFile File { get; set; }
            public DateTime LastModified { get; set; }
        }
    }
}