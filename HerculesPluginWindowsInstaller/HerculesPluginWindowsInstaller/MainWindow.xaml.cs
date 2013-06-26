using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Net;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Configuration;

namespace HerculesPluginWindowsInstaller
{
    public partial class MainWindow : Window
    {
        private Boolean connectionReady;
        
        public MainWindow()
        {
            InitializeComponent();
            
            connectionReady = false;

            readAndSetConfiguration();
        }

        public void readAndSetConfiguration()
        {
            serverAddressShow.Text = ConfigurationManager.AppSettings["serverAddress"];
            serverAddress.Text = ConfigurationManager.AppSettings["serverAddress"];

            installFolder.Text = ConfigurationManager.AppSettings["installFolder"];
            downloadFolder.Text = ConfigurationManager.AppSettings["downloadFolder"];

            proxyAddress.Text = ConfigurationManager.AppSettings["proxyAddress"];
            proxyPort.Text = ConfigurationManager.AppSettings["proxyPort"];
            proxyLogin.Text = ConfigurationManager.AppSettings["proxyLogin"];
            proxyPassword.Password = ConfigurationManager.AppSettings["proxyPassword"];
            proxyEnable.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["proxyEnable"]);
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            WebClient webClient = getWebClient();
            try
            {

                using (XmlReader reader = XmlReader.Create(getWebClient().OpenRead(serverAddressShow.Text)))
                {
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    lstFeedItems.ItemsSource = feed.Items;

                    connectionReady = true;

                    Log.Content += "Plugins loading from deposit: \n" + serverAddress.Text + "\n";
                }
            }
            catch (WebException web)
            {
                MessageBox.Show("No internet connection :(");
            }
        }

        private WebClient getWebClient()
        {
            WebClient webClient = new WebClient();

            if (ConfigurationManager.AppSettings["proxyAddress"] != null
                    && ConfigurationManager.AppSettings["proxyPort"] != null
                    && proxyEnable.IsChecked == true)
            {
                var proxy = new WebProxy(ConfigurationManager.AppSettings["proxyAddress"], Convert.ToInt32(ConfigurationManager.AppSettings["proxyPort"]));

                if (ConfigurationManager.AppSettings["proxyLogin"] != null
                    && ConfigurationManager.AppSettings["proxyPassword"] != null)
                {
                    proxy.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["proxyLogin"], ConfigurationManager.AppSettings["proxyPassword"]);
                    webClient.Proxy = proxy;
                }
                else
                {
                    webClient.Proxy = proxy;
                }
            }

            return webClient;
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (connectionReady)
            {
                SyndicationItem item = (SyndicationItem)lstFeedItems.SelectedItem;

                WebClient webClient = getWebClient();

                Log.Content += "Download of " + item.Id + " started\n";
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                webClient.DownloadFileAsync(item.Links[0].Uri, downloadFolder.Text + item.Id);

                String itemFolder = item.Id.Substring(0, item.Id.Length - 4);
                System.IO.Directory.CreateDirectory(installFolder.Text + itemFolder);
            }
            else
            {
                MessageBox.Show("No internet connection or no contents loaded :("); 
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            SyndicationItem item = (SyndicationItem)lstFeedItems.SelectedItem;
            String itemFolder = item.Id.Substring(0, item.Id.Length - 4);

            Log.Content += "Download of " + item.Id + " finished\n";
            Log.Content += "Extraction of " + item.Id + " started\n";
            dezip(downloadFolder.Text + item.Id, installFolder.Text + itemFolder);
            Log.Content += "Extraction of " + item.Id + " finished\n";
            MessageBox.Show("Installation completed!");
        }


        public static void dezip(String zipPath, String extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(@zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!(System.IO.File.Exists(System.IO.Path.Combine(extractPath, entry.FullName))))
                    {
                        entry.ExtractToFile(System.IO.Path.Combine(extractPath, entry.FullName));
                    }
                }
            }

        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings.Remove("serverAddress");
            config.AppSettings.Settings.Add("serverAddress", serverAddress.Text);

            config.AppSettings.Settings.Remove("downloadFolder");
            config.AppSettings.Settings.Add("downloadFolder", downloadFolder.Text);

            config.AppSettings.Settings.Remove("installFolder");
            config.AppSettings.Settings.Add("installFolder", installFolder.Text);

            config.AppSettings.Settings.Remove("proxyAddress");
            config.AppSettings.Settings.Add("proxyAddress", proxyAddress.Text);

            config.AppSettings.Settings.Remove("proxyPort");
            config.AppSettings.Settings.Add("proxyPort", proxyPort.Text);

            config.AppSettings.Settings.Remove("proxyLogin");
            config.AppSettings.Settings.Add("proxyLogin", proxyLogin.Text);

            config.AppSettings.Settings.Remove("proxyPassword");
            config.AppSettings.Settings.Add("proxyPassword", proxyPassword.Password);

            config.AppSettings.Settings.Remove("proxyEnable");
            config.AppSettings.Settings.Add("proxyEnable", Convert.ToString(proxyEnable.IsChecked));

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            readAndSetConfiguration();

            MessageBox.Show("Configuration saved!");
        }
    }
}