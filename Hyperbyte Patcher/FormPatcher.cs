using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hyperbyte_Patcher
{
    public partial class FormPatcher : Form
    {
        private bool enableNotice;
        private bool patcherIsDownloading;
        private bool updateCompleted;
        private uint patchVersion;
        private uint hyperVersion;
        private string appFileName;
        private string appArguments;
        private string noticeText;
        private string windowTitle;
        private Uri patchesWebPath;
        private WebClient webClient;
        private Package packageOnDownloading;
        private DirectoryInfo hyperFolder;
        private DirectoryInfo tempFolder;
        private Stopwatch stopwatch;
        private KeyValueConfigurationCollection hyperSettings;
        private Configuration hyperConfigFile;

        public FormPatcher(bool enableNotice, string windowTitle, string appFileName, string appArguments)
        {
            this.enableNotice = enableNotice;
            this.appFileName = appFileName;
            this.appArguments = appArguments;
            this.windowTitle = windowTitle;
            hyperConfigFile = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            hyperSettings = hyperConfigFile.AppSettings.Settings;
            hyperFolder = new DirectoryInfo(Environment.CurrentDirectory);
            stopwatch = new Stopwatch();
            InitializeComponent();
            buttonStartApp.Enabled = false;
            labelSpeed.Hide();
            this.Text = windowTitle;
            labelSpeed.Text = string.Empty;
            bool checkboxStatus;
            try
            {
                checkboxStatus = Convert.ToBoolean(hyperSettings["autostart"].Value);
                patchVersion = Convert.ToUInt32(hyperSettings["patchVersion"].Value);
                hyperVersion = Convert.ToUInt32(hyperSettings["hyperVersion"].Value);
                patchesWebPath = new Uri(hyperSettings["patchesWebPath"].Value);
            }
            catch (Exception)
            {
                checkboxStatus = true;
                patchVersion = 0;
                hyperVersion = 0;
            }
            checkBoxAutoStart.Checked = checkboxStatus;

            if (enableNotice == true)
                DownloadNoticeTexts();
            else
                TransformMini();

            InitPatchProcess();
        }

        private void DownloadNoticeTexts()
        {
            labelStatus.Text = "Downloading Notices.";
            try
            {
                noticeText = web2string(Path.Combine(patchesWebPath.AbsoluteUri, "notice"));
                textBoxNotice.Text = noticeText;
            }
            catch (Exception e)
            {
                textBoxNotice.Text = e.Message;
            }
        }

        private void InitPatchProcess()
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

        private List<Package> BuildDownloadList(string patchlist)
        {
            var filelist = new List<Package>();
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

                    if ((package.Name.Contains(".zip")) && package.Version > patchVersion) //autoupdate files
                        filelist.Add(package);

                    else if (package.Name.Contains(".hyp") && package.Version > hyperVersion) //autoupdate patcher
                        HyperbyteUpdate();
                }
            }
            return filelist;
        }

        private async void DownloadFiles(List<Package> packagelist)
        {
            if (packagelist != null)
            {
                labelStatus.Text = "Attempt to download files. Please wait.";
                tempFolder = Directory.CreateDirectory("temp");
                CleanFiles(tempFolder);
                patcherIsDownloading = false;
                updateCompleted = false;
                var count = 0;

                while (!updateCompleted)
                {
                    foreach (var package in packagelist)
                    {
                        count++;
                        package.Localization = Path.Combine(tempFolder.FullName, package.Name);

                        while (patcherIsDownloading)
                            await Task.Delay(1000);

                        if (!package.Downloaded)
                        {
                            using (webClient = new WebClient())
                            {
                                stopwatch.Start();
                                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                                labelStatus.Text = "Fetching " + package.Name;
                                this.Text = string.Format("[{1}/{2}] {0}", windowTitle, count, packagelist.Count);
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
                    }

                    while (patcherIsDownloading)
                        await Task.Delay(1000);

                    updateCompleted = true;
                    RecheckPackages(packagelist);
                }
                FinishPatchProcess();
            }
            return;
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            labelSpeed.Text = string.Format("{0} kb/s", ((e.BytesReceived / 1024d) / stopwatch.Elapsed.TotalSeconds).ToString("0.00")); //must test
            labelStatus.Text = string.Format("Downloading {0} [{1} MBs / {2} MBs ({3}%)]", packageOnDownloading.Name, (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"), e.ProgressPercentage.ToString());
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            var failure = e.Error;

            if (failure == null)
            {
                stopwatch.Reset();
                packageOnDownloading.Downloaded = true;
                ExtractPackage(packageOnDownloading);
                patchVersion = packageOnDownloading.Version;
                Write2ConfigFile("patchVersion", patchVersion.ToString());
                labelStatus.Text = packageOnDownloading.Name + " has been installed.";
                patcherIsDownloading = false;
            }
            else
                labelStatus.Text = string.Format("Failed to Get {0}\n{1}", packageOnDownloading.Name, failure.Message);
        }

        private void FinishPatchProcess()
        {
            CleanDirectory(tempFolder);
            Write2ConfigFile("patchVersion", patchVersion.ToString());
            this.Text = string.Format("[Completed] {0}", windowTitle);
            progressBar.Value = progressBar.Maximum;
            labelStatus.Text = "Patch Process Completed.";
            buttonStartApp.Enabled = true;
            if (checkBoxAutoStart.Checked)
                StartApplication();
        }

        private static string web2string(string url)
        {
            var sb = new StringBuilder();
            var buffer = new byte[8192];
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

        private void RecheckPackages(List<Package> packagelist)
        {
            labelStatus.Text = "Rechecking Packages.";
            foreach (var pkg in packagelist)
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

        private void TransformMini()
        {
            panelNotice.Hide();
            panelMain.Location = new System.Drawing.Point(13, 13);
            Size = new System.Drawing.Size(500, 170);
        }

        private void buttonStartApp_Click(object sender, EventArgs e)
        {
            Write2ConfigFile("autostart", checkBoxAutoStart.Checked.ToString());
            StartApplication();
        }

        private void buttonClosePatcher_Click(object sender, EventArgs e)
        {
            Write2ConfigFile("autostart", checkBoxAutoStart.Checked.ToString());
            Environment.Exit(0);
        }

        private void HyperbyteUpdate()
        {
            try
            {
                var selfupdate = new Process();
                selfupdate.StartInfo.FileName = "selfupdater.exe";
                selfupdate.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open selfupdater.exe\n" + e.Message, "HyperbyteUpdate Error");
                Environment.Exit(0);
            }
        }

        private void StartApplication()
        {
            try
            {
                Process application = new Process();
                application.StartInfo.FileName = appFileName;
                application.StartInfo.Arguments = appArguments;
                application.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open " + appFileName + "\nError: " + e.Message, "StartApplication Error");
            }
        }

        private void Write2ConfigFile(string key, string value)
        {
            hyperSettings.Remove(key);
            hyperSettings.Add(key, value);
            hyperConfigFile.Save(ConfigurationSaveMode.Modified);
        }
    }
}
