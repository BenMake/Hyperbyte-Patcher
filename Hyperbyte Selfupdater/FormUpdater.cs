using Hyperbyte_Patcher;
using Ionic.Zip;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hyperbyte_Selfupdater
{
    public partial class FormUpdater : Form
    {
        private bool patcherIsDownloading;
        private bool updateCompleted;
        private uint hyperVersion;
        private string patcherExecutable;
        private string patcherArguments;
        private Uri patchesWebPath;
        private WebClient webClient;
        private Package packageOnDownloading;
        private DirectoryInfo hyperFolder;
        private DirectoryInfo tempFolder;
        private KeyValueConfigurationCollection hyperSettings;
        private Configuration hyperConfigFile;

        public FormUpdater(string patcherExecutable, string patcherArguments)
        {
            this.patcherExecutable = patcherExecutable;
            this.patcherArguments = patcherArguments;
            hyperConfigFile = ConfigurationManager.OpenExeConfiguration(patcherExecutable);
            hyperSettings = hyperConfigFile.AppSettings.Settings;
            hyperFolder = new DirectoryInfo(Environment.CurrentDirectory);
            InitializeComponent();
            try
            {
                hyperVersion = Convert.ToUInt32(hyperSettings["hyperVersion"].Value);
                patchesWebPath = new Uri(hyperSettings["patchesWebPath"].Value);
            }
            catch (Exception)
            {
                hyperVersion = 0;
            }

            InitUpdateProcess();
        }

        private void InitUpdateProcess()
        {
            labelStatus.Text = "Downloading patch list.";
            string patchlist;
            try
            {
                patchlist = web2string(Path.Combine(patchesWebPath.AbsoluteUri, "patchlist"));
            }
            catch (Exception e)
            {
                labelStatus.Text = "Failed to Get the patch list.\n" + e.Message;
                return;
            }

            labelStatus.Text = "Building download list.";
            var packagelist = BuildDownloadList(patchlist);

            DownloadFiles(packagelist);
        }

        private Package BuildDownloadList(string patchlist)
        {
            Package package2download = null;
            using (var reader = new StringReader(patchlist))
            {
                var line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    var splitline = line.Split('\t');
                    var package = new Package();
                    try
                    {
                        package.Version = Convert.ToUInt32(splitline[0]);
                        package.Name = splitline[1];
                    }
                    catch (Exception e)
                    {
                        labelStatus.Text = "Patch list is in wrong format.\n" + e.Message;
                        return null;
                    }
                    if (package.Name.Contains(".hyp") && package.Version > hyperVersion) //select the last one (newer)
                        package2download = package;
                }
            }
            return package2download;
        }

        private void FinishPatchProcess()
        {
            CleanDirectory(tempFolder);
            WriteConfigFile("hyperVersion", hyperVersion.ToString());
            progressBar.Value = progressBar.Maximum;
            StartPatcher();
        }

        private async void DownloadFiles(Package packagelist)
        {
            if (packagelist != null)
            {
                labelStatus.Text = "Attempt to download files. Please wait.";
                tempFolder = Directory.CreateDirectory("temp");
                CleanFiles(tempFolder);
                patcherIsDownloading = false;
                updateCompleted = false;

                while (!updateCompleted)
                {
                    var package = packagelist;

                    package.Localization = Path.Combine(tempFolder.FullName, package.Name);

                    while (patcherIsDownloading)
                        await Task.Delay(1000);

                    if (!package.Downloaded)
                    {
                        using (webClient = new WebClient())
                        {
                            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                            labelStatus.Text = "Fetching " + package.Name;
                            try
                            {
                                var fileaddress = new Uri(patchesWebPath.AbsoluteUri + package.Name);
                                webClient.DownloadFileAsync(fileaddress, package.Localization);
                                packageOnDownloading = package;
                                patcherIsDownloading = true;
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                                return;
                            }
                        }
                    }
                    while (patcherIsDownloading)
                        await Task.Delay(1000);

                    updateCompleted = true;
                    RecheckPackages(package);
                }
                FinishPatchProcess();
            }
            else
                StartPatcher();
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            labelStatus.Text = string.Format("Downloading {0} [{1} MBs / {2} MBs ({3}%)]", packageOnDownloading.Name, (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"), e.ProgressPercentage.ToString());
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            var failure = e.Error;

            if (failure == null)
            {
                packageOnDownloading.Downloaded = true;
                ExtractPackage(packageOnDownloading);
                hyperVersion = packageOnDownloading.Version;
                WriteConfigFile("hyperVersion", hyperVersion.ToString());
                labelStatus.Text = packageOnDownloading.Name + " has been installed.";
                patcherIsDownloading = false;
            }
            else
            {
                labelStatus.Text = string.Format("Failed to Get {0}\n{1}", packageOnDownloading.Name, failure.Message);
            }
        }

        private static string web2string(string url)
        {
            var sb = new StringBuilder();
            byte[] buffer = new byte[8192];
            var request = (HttpWebRequest)WebRequest.Create(url);
            var noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            var response = (HttpWebResponse)request.GetResponse();
            var resStream = response.GetResponseStream();
            var tempString = string.Empty;
            var count = 0;
            do
            {
                count = resStream.Read(buffer, 0, buffer.Length);
                if (count != 0)
                {
                    tempString = Encoding.UTF8.GetString(buffer, 0, count);
                    sb.Append(tempString);
                }
            }
            while (count > 0);
            return sb.ToString();
        }

        private void ExtractPackage(Package package)
        {
            labelStatus.Text = "Extracting " + packageOnDownloading.Name;
            try
            {
                var packagelocation = Path.Combine(tempFolder.FullName, package.Name);
                using (var zippkg = ZipFile.Read(packagelocation))
                    foreach (var entry in zippkg)
                        entry.Extract(hyperFolder.FullName, ExtractExistingFileAction.OverwriteSilently);
                package.Extracted = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to extract {0}\nError: {1}", package.Name, e.Message), "ExtractPackage");
                CleanDirectory(tempFolder);
                Environment.Exit(0);
            }
        }

        private void RecheckPackages(Package packagelist)
        {
            labelStatus.Text = "Rechecking Packages.";
            var pkg = packagelist;
            if (pkg.Downloaded == false || pkg.Extracted == false)
            {
                updateCompleted = false;
                return;
            }
        }

        private void CleanDirectory(DirectoryInfo directory)
        {
            labelStatus.Text = "Cleaning directories.";
            try
            {
                foreach (var file in directory.GetFiles())
                    file.Delete();
                foreach (var dir in directory.GetDirectories())
                    dir.Delete(true);

                directory.Delete();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "CleanDirectory Error");
                Environment.Exit(0);
            }
        }

        private void CleanFiles(DirectoryInfo directory)
        {
            labelStatus.Text = "Cleaning files.";
            try
            {
                foreach (var file in directory.GetFiles())
                    file.Delete();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "CleanFiles Error");
                Environment.Exit(0);
            }
        }

        private void StartPatcher()
        {
            try
            {
                Process application = new Process();
                application.StartInfo.FileName = patcherExecutable;
                application.StartInfo.Arguments = patcherArguments;
                application.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open " + patcherExecutable + "\nError: " + e.Message, "StartApplication Error");
                Environment.Exit(0);
            }
        }

        private void WriteConfigFile(string key, string value)
        {
            try
            {
                hyperSettings.Remove(key);
                hyperSettings.Add(key, value);
                hyperConfigFile.Save(ConfigurationSaveMode.Modified);
            } catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "Write2ConfigFile Error");
                Environment.Exit(0);
            }
        }
    }
}
