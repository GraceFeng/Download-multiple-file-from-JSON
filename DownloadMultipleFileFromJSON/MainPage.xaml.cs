using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Store;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DownloadMultipleFileFromJSON
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<BukuAudio> datasource = new ObservableCollection<BukuAudio>();
        private LicenseChangedEventHandler licenseChangeHandler = null;
        private ListingInformation listing = null;
        private BukuAudio itemDetail = null;
        private LicenseInformation licenseInformation = CurrentAppSimulator.LicenseInformation;
        private DataTransferManager dataTransferManager;

        public MainPage()
        {
            this.InitializeComponent();
            StoreAll();
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        ///
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //navigationHelper.OnNavigatedTo(e);

            this.dataTransferManager = DataTransferManager.GetForCurrentView();
            this.dataTransferManager.DataRequested +=
                new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>
                    (this.OnDataRequested);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //navigationHelper.OnNavigatedFrom(e);

            this.dataTransferManager.DataRequested -=
                new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>
                    (this.OnDataRequested);
        }

        #endregion NavigationHelper registration

        public async void StoreAll()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            if (connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                busyindicator.IsActive = true;
                //await LoadInAppPurchaseProxyFileAsync();
                try
                {
                    var client = new Windows.Web.Http.HttpClient();
                    string urlPath = "http://mhndt.com/newsstand/renungan-harian/callback/allWinItems";
                    var values = new List<KeyValuePair<string, string>>
                    {
                        //new KeyValuePair<string, string>("hal", "1"),
                        //new KeyValuePair<string, string>("limit","300")
                    };

                    var response = await client.PostAsync(new Uri(urlPath), new Windows.Web.Http.HttpFormUrlEncodedContent(values));
                    response.EnsureSuccessStatusCode();

                    if (!response.IsSuccessStatusCode)
                    {
                        RequestException();
                    }

                    string jsonText = await response.Content.ReadAsStringAsync();
                    JsonObject jsonObject = JsonObject.Parse(jsonText);
                    JsonArray jsonData1 = jsonObject["data"].GetArray();

                    foreach (JsonValue groupValue in jsonData1)
                    {
                        JsonObject groupObject = groupValue.GetObject();
                        string bundleName = "";
                        string pathFile = "";

                        string nid = groupObject["sku"].GetString();
                        string title = groupObject["judul"].GetString();
                        string deskripsi = groupObject["deskripsi"].GetString();
                        string tanggal = groupObject["tgl"].GetString();
                        string tipe = groupObject["tipe"].GetString();
                        string namaTipe = groupObject["nama_tipe"].GetString();
                        string gratis = groupObject["gratis"].GetString();
                        string dataFile = groupObject["nfile"].GetString();
                        string harga = groupObject["hrg"].GetString();
                        var bundleObj = groupObject["bundle"];

                        BukuAudio file = new BukuAudio();

                        file.BundleName = new List<string>();
                        file.BundlePath = new List<string>();
                        if (bundleObj.ValueType == JsonValueType.Array)
                        {
                            JsonArray bundle = bundleObj.GetArray();
                            foreach (JsonValue groupValue1 in bundle)
                            {
                                JsonObject groupObject1 = groupValue1.GetObject();

                                bundleName = groupObject1["bundle_file"].GetString();
                                pathFile = groupObject1["path_file"].GetString();

                                file.BundleName.Add(bundleName);
                                file.Tipe = tipe;
                                if (file.Tipe == "0")
                                {
                                    file.BundlePath.Add(pathFile + bundleName + ".pdf");
                                    //file.BundlePath = pathFile + bundleName + ".pdf";
                                }
                                else if (file.Tipe == "1")
                                {
                                    file.BundlePath.Add(pathFile + bundleName);
                                    //file.BundlePath = pathFile + bundleName;
                                }
                            }
                        }

                        file.SKU = nid;
                        file.Judul = title;
                        file.Deskripsi = deskripsi;

                        string[] formats = { "d MMMM yyyy" };
                        var dateTime = DateTime.ParseExact(tanggal, formats, new CultureInfo("id-ID"), DateTimeStyles.None);

                        Int64 n = Int64.Parse(dateTime.ToString("yyyyMMdd"));
                        file.Tanggal = n.ToString();
                        int tgl = Int32.Parse(file.Tanggal);
                        //file.Tipe = tipe;
                        file.NamaTipe = "Tipe: " + namaTipe;
                        file.Gratis = gratis;
                        file.File = "http://mhndt.com/newsstand/rh/item/" + dataFile;
                        file.Cover = "http://mhndt.com/newsstand/rh/item/" + dataFile + ".jpg";

                        if (licenseInformation.ProductLicenses[file.SKU].IsActive)
                        {
                            file.Harga = "Purchased";
                        }
                        else
                        {
                            if (file.Gratis == "1")
                            {
                                file.Harga = "Free";
                            }
                            else
                            {
                                file.Harga = harga;
                            }
                        }

                        if (bundleObj.ValueType == JsonValueType.Array && tgl >= 20150301
                            || file.Judul == "Renungan Harian Tahunan VII"
                            || file.Judul == "Edisi Tahunan VI" || file.Judul == "RH Anak Volume 01 : Yesus Sahabatku")
                        {
                            datasource.Add(file);
                        }
                    }

                    if (jsonData1.Count > 0)
                    {
                        itemGridView.ItemsSource = datasource;
                    }
                    else
                    {
                        MessageDialog messageDialog;
                        messageDialog = new MessageDialog("Data kosong", "Buku atau Audio Tidak tersedia");
                        messageDialog.Commands.Add(new UICommand("Tutup", (command) =>
                        {
                            this.Frame.Navigate(typeof(MainPage));
                        }));
                        await messageDialog.ShowAsync();
                    }
                }
                catch (HttpRequestException ex)
                {
                    RequestException();
                    busyindicator.IsActive = false;
                }
            }
            else
            {
                ConnectionException();
                busyindicator.IsActive = false;
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

        private async void RequestException()
        {
            MessageDialog messageDialog;
            messageDialog = new MessageDialog("Gangguan Server!", "Request Exception!");
            messageDialog.Commands.Add(new UICommand("Tutup", (command) =>
            {
                this.Frame.Navigate(typeof(MainPage));
            }));

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        private async Task LoadInAppPurchaseProxyFileAsync()
        {
            StorageFolder proxyDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile proxyFile = await proxyDataFolder.GetFileAsync("in-app-purchase.xml");
            licenseChangeHandler = new LicenseChangedEventHandler(InAppPurchaseRefreshScenario);
            CurrentAppSimulator.LicenseInformation.LicenseChanged += licenseChangeHandler;
            await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);

            try
            {
                listing = await CurrentAppSimulator.LoadListingInformationAsync();
            }
            catch (Exception)
            {
                var messageDialog = new MessageDialog("LoadListingInformationAsync API gagal");
                var task = messageDialog.ShowAsync().AsTask();
            }
        }

        private async Task LoadInAppPurchaseDataAsync()
        {
            try
            {
                ListingInformation listing = await CurrentAppSimulator.LoadListingInformationAsync();
                //var product1 = listing.ProductListings["product1"];
                //var product2 = listing.ProductListings["product2"];
            }
            catch (Exception)
            {
                var messageDialog = new MessageDialog("LoadListingInformationAsync API gagal");
                var task = messageDialog.ShowAsync().AsTask();
            }
        }

        private void InAppPurchaseRefreshScenario()
        {
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ProductDetail.IsOpen = true;
            itemDetail = e.ClickedItem as BukuAudio;
            //Debug.WriteLine("id:" + e.ClickedItem);
            DetailCover.Source = new BitmapImage(new Uri(itemDetail.Cover, UriKind.Absolute));
            DetailJudul.Text = itemDetail.Judul;
            DetailDeskripsi.Text = itemDetail.Deskripsi;
            DetailSKU.Text = itemDetail.SKU;
            DetailHarga.Text = itemDetail.Harga;
            DetailFree.Text = itemDetail.Gratis;
            DetailTipe.Text = itemDetail.Tipe;
            DetailNamaTipe.Text = itemDetail.NamaTipe;
            DetailBundleName.Text = itemDetail.BundleName == null ? "null" : itemDetail.BundleName.FirstOrDefault();
            DetailPathFile.Text = itemDetail.BundlePath == null ? "null" : itemDetail.BundlePath.FirstOrDefault();
        }

        private async void downloadClicked(object sender, RoutedEventArgs e)
        {
            ProductDetail.IsOpen = false;
            Uri uri = new Uri(itemDetail.BundlePath.FirstOrDefault());
            //foreach (var path in itemDetail.BundlePath)
            //{
            //    uri = new Uri(path);
            //}
            string filename = System.IO.Path.GetFileName(uri.LocalPath);
            if (itemDetail.NamaTipe == "Tipe: audio")
            {
                string statustext = String.Format("Download Audio '{0}'?", itemDetail.Judul);
                string sudahada = String.Format("Audio '{0}' sudah ada/sedang didownload", itemDetail.Judul);
                MessageDialog messageDialog;
                try
                {
                    StorageFolder library = await ApplicationData.Current.LocalFolder.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
                    var file = await library.GetFileAsync(filename);

                    messageDialog = new MessageDialog(sudahada, "Audio sudah ada");
                    messageDialog.Commands.Add(new UICommand("Library", (command) =>
                    {
                        this.Frame.Navigate(typeof(library.LibraryPage));
                    }));
                    messageDialog.Commands.Add(new UICommand("Batal", (command) =>
                    {
                        //rootPage.NotifyUser("The 'Don't install' command has been selected.", NotifyType.StatusMessage);
                    }));
                }
                catch (FileNotFoundException ex)
                {
                    //file not exists show download dialog
                    // Create the message dialog and set its content and title

                    messageDialog = new MessageDialog(statustext, "Download");
                    // Add commands and set their callbacks

                    messageDialog.Commands.Add(new UICommand("Download", (command) =>
                    {
                        this.Frame.Navigate(typeof(library.LibraryPage), itemDetail);
                    }));

                    messageDialog.Commands.Add(new UICommand("Batal", (command) =>
                    {
                        //rootPage.NotifyUser("The 'Don't install' command has been selected.", NotifyType.StatusMessage);
                    }));
                }
                // Show the message dialog
                await messageDialog.ShowAsync();
            }
            else if (itemDetail.NamaTipe == "Tipe: magazine")
            {
                string statustext = String.Format("Download Buku '{0}'?", itemDetail.Judul);
                string sudahada = String.Format("Buku '{0}' sudah ada/sedang didownload", itemDetail.Judul);
                MessageDialog messageDialog;
                try
                {
                    StorageFolder library = await ApplicationData.Current.LocalFolder.CreateFolderAsync("library", CreationCollisionOption.OpenIfExists);
                    var file = await library.GetFileAsync(filename);

                    messageDialog = new MessageDialog(sudahada, "Buku sudah ada");
                    messageDialog.Commands.Add(new UICommand("Library", (command) =>
                    {
                        this.Frame.Navigate(typeof(library.LibraryPage));
                    }));
                    messageDialog.Commands.Add(new UICommand("Batal", (command) =>
                    {
                        //rootPage.NotifyUser("The 'Don't install' command has been selected.", NotifyType.StatusMessage);
                    }));
                }
                catch (FileNotFoundException ex)
                {
                    //file not exists show download dialog
                    // Create the message dialog and set its content and title

                    messageDialog = new MessageDialog(statustext, "Download");
                    // Add commands and set their callbacks

                    messageDialog.Commands.Add(new UICommand("Download", (command) =>
                    {
                        this.Frame.Navigate(typeof(library.LibraryPage), itemDetail);
                    }));

                    messageDialog.Commands.Add(new UICommand("Batal", (command) =>
                    {
                        //rootPage.NotifyUser("The 'Don't install' command has been selected.", NotifyType.StatusMessage);
                    }));
                }
                //Show the message dialog
                await messageDialog.ShowAsync();
            }
        }

        protected void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            // Call the scenario specific function to populate the datapackage with the data to be shared.
            GetShareContent(e.Request);
        }

        private void shareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void GetShareContent(DataRequest request)
        {
            TextToShare.Text = "Dapatkan buku/audio '" + DetailJudul.Text + "' download di aplikasi Renungan Harian";
            string dataPackageText = TextToShare.Text;
            if (!String.IsNullOrEmpty(dataPackageText))
            {
                DataPackage requestData = request.Data;
                requestData.Properties.Title = "Renungan Harian";
                requestData.Properties.Description = "Share buku/audio '" + DetailJudul.Text + "'"; // The description is optional.
                                                                                                    //requestData.SetUri(new Uri("http://majalah.id/majalah/id/" + DetailSlug.Text));
                requestData.SetText(dataPackageText);
            }
        }

        private void cancelClicked(object sender, RoutedEventArgs e)
        {
            ProductDetail.IsOpen = false;
        }

        private void cover_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            busyindicator.IsActive = true;
        }

        private void cover_Loaded(object sender, RoutedEventArgs e)
        {
            busyindicator.IsActive = false;
        }

        private void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            busyindicator.IsActive = false;
        }

        private void cover_Loading(FrameworkElement sender, object args)
        {
            busyindicator.IsActive = true;
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}