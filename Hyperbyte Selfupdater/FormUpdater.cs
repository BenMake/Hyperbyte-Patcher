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
        private bool patcherDownloading;
        private bool downloadCompleted;
        private string patcherexecutable;
        private string patcherarguments;
        private KeyValueConfigurationCollection hyperSettings;
        private Configuration hyperConfigFile = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
        private uint hyperversion;
        private Uri webpath;
        private WebClient webclient;
        private Package pkgDownloading;
        private DirectoryInfo patcherfolder;
        private DirectoryInfo tempfolder;
        private DirectoryInfo path2extract;

        public FormUpdater(string patcherexecutable, string patcherarguments)
        {
            this.patcherexecutable = patcherexecutable;
            this.patcherarguments = patcherarguments;
            hyperSettings = hyperConfigFile.AppSettings.Settings;
            InitializeComponent();
            try
            {
                hyperversion = Convert.ToUInt32(hyperSettings["hyperversion"].Value);
                webpath = new Uri(hyperSettings["webpath"].Value);
            }
            catch (Exception)
            {
                hyperversion = 0;
            }

            InitUpdateProcess();
        }

        private void InitUpdateProcess()
        {
            labelStatus.Text = "Downloading patch list.";
            string patchlist;
            try
            {
                patchlist = web2string(webpath.AbsoluteUri + "patchlist");
            }
            catch (Exception e)
            {
                labelStatus.Text = "Failed to Get the patch list.\n" + e.Message;
                return;
            }

            labelStatus.Text = "Building download list.";
            var package2download = BuildDownloadList(patchlist);

            DownloadFiles(package2download);
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
                    if (package.Name.Contains(".hyp") && package.Version > hyperversion) //select the last one (newer)
                        package2download = package;
                }
            }
            return package2download;
        }

        private void FinishPatchProcess()
        {
            CleanDirectory(tempfolder);
            WriteConfigFile("hyperversion", hyperversion.ToString());
            progressBar.Value = progressBar.Maximum;
            StartPatcher();
        }

        private async void DownloadFiles(Package package2download)
        {
            if (package2download != null)
            {
                labelStatus.Text = "Attempt to download files. Please wait.";
                tempfolder = Directory.CreateDirectory("temp");
                path2extract = Directory.CreateDirectory("tempext");
                patcherfolder = Directory.GetParent(tempfolder.Name);
                CleanFiles(tempfolder);
                CleanFiles(path2extract);
                patcherDownloading = false;
                downloadCompleted = false;

                while (!downloadCompleted)
                {
                    var package = package2download;

                    package.Localization = tempfolder.FullName + "\\" + package.Name;

                    while (patcherDownloading)
                        await Task.Delay(1000);

                    if (!package.Downloaded)
                    {
                        using (webclient = new WebClient())
                        {
                            webclient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                            webclient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                            labelStatus.Text = "Fetching " + package.Name;
                            try
                            {
                                var fileaddress = new Uri(webpath.AbsoluteUri + package.Name);
                                webclient.DownloadFileAsync(fileaddress, package.Localization);
                                pkgDownloading = package;
                                patcherDownloading = true;
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                                return;
                            }
                        }
                    }
                    while (patcherDownloading)
                        await Task.Delay(1000);

                    downloadCompleted = true;
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
            labelStatus.Text = string.Format("Downloading {0} [{1} MBs / {2} MBs ({3}%)]", pkgDownloading.Name, (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"), e.ProgressPercentage.ToString());
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            var failure = e.Error;

            if (failure == null)
            {
                pkgDownloading.Downloaded = true;
                ExtractPackage(pkgDownloading);
                CleanDirectory(path2extract);
                hyperversion = pkgDownloading.Version;
                WriteConfigFile("hyperversion", hyperversion.ToString());
                labelStatus.Text = pkgDownloading.Name + " has been installed.";
                patcherDownloading = false;
            }
            else
            {
                labelStatus.Text = string.Format("Failed to Get {0}\n{1}", pkgDownloading.Name, failure.Message);
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
            labelStatus.Text = "Extracting " + pkgDownloading.Name;
            try
            {
                var packagelocation = Path.Combine(tempfolder.FullName, package.Name);
                using (ZipFile zip = ZipFile.Read(packagelocation))
                {
                    foreach (ZipEntry entry in zip)
                        entry.Extract(patcherfolder.FullName, ExtractExistingFileAction.OverwriteSilently);
                }
                package.Extracted = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to extract {0}\nError: {1}", package.Name, e.Message), "ExtractPackage");
                CleanDirectory(tempfolder);
                CleanDirectory(path2extract);
                Environment.Exit(0);
            }
        }

        private void ReplaceFile(DirectoryInfo source, DirectoryInfo destination)
        {
            labelStatus.Text = "Replacing files.";
            var sourcefiles = source.GetFiles();
            var destinationfiles = destination.GetFiles();

            foreach (FileInfo sourcefile in sourcefiles)
            {
                foreach (FileInfo destinationfile in destinationfiles)
                {
                    if (sourcefile.Name.Equals(destinationfile.Name))
                    {
                        var sourcefilepath = sourcefile.FullName;
                        var destinationfilepath = destinationfile.FullName;
                        var backupfile = Path.Combine(tempfolder.FullName, source.Name + ".bkp");
                        File.Replace(sourcefilepath, destinationfilepath, backupfile);
                    }
                }
                if (File.Exists(sourcefile.FullName))
                {
                    var file2move = Path.Combine(patcherfolder.FullName, sourcefile.Name);
                    File.Move(sourcefile.FullName, file2move);
                }
            }

        }

        private void RecheckPackages(Package package2download)
        {
            labelStatus.Text = "Rechecking Packages.";
            var pkg = package2download;
            if (pkg.Downloaded == false || pkg.Extracted == false)
            {
                downloadCompleted = false;
                return;
            }
        }

        private void CleanDirectory(DirectoryInfo directory)
        {
            labelStatus.Text = "Cleaning stuff.";
            try
            {
                foreach (FileInfo file in directory.GetFiles())
                    if (File.Exists(file.FullName))
                        file.Delete();

                foreach (DirectoryInfo dir in directory.GetDirectories())
                    if (Directory.Exists(directory.FullName))
                        dir.Delete(true);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "CleanDirectory Error");
                Environment.Exit(0);
            }
        }

        private void CleanFiles(DirectoryInfo directory)
        {
            labelStatus.Text = "Cleaning stuff.";
            try
            {
                if (Directory.Exists(directory.FullName))
                {
                    foreach (FileInfo file in directory.GetFiles())
                    {
                        if (File.Exists(file.FullName))
                            file.Delete();
                    }
                }
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
                application.StartInfo.FileName = patcherexecutable;
                application.StartInfo.Arguments = patcherarguments;
                application.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open " + patcherexecutable + "\nError: " + e.Message, "StartApplication Error");
                Environment.Exit(0);
            }
        }

        private void WriteConfigFile(string key, string value)
        {
            hyperSettings.Remove(key);
            hyperSettings.Add(key, value);
            hyperConfigFile.Save(ConfigurationSaveMode.Modified);
        }


    }
}
