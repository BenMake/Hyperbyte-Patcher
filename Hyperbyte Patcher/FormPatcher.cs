using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        private bool patcherDownloading;
        private bool downloadCompleted;
        private uint patchversion;
        private uint hyperversion;
        private string filename;
        private string arguments;
        private string notice;
        private string windowtitle;
        private Uri webpatch;
        private WebClient webclient;
        private Package pkgDownloading;
        private DirectoryInfo patchfolder;
        private DirectoryInfo tempfolder;
        private DirectoryInfo path2extract;
        private Stopwatch sw = new Stopwatch();
        private KeyValueConfigurationCollection hyperSettings;
        private Configuration hyperConfigFile = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

        public FormPatcher(bool enableNotice, string windowtitle, string filename, string arguments)
        {
            this.enableNotice = enableNotice;
            this.filename = filename;
            this.arguments = arguments;
            this.windowtitle = windowtitle;
            hyperSettings = hyperConfigFile.AppSettings.Settings;
            InitializeComponent();
            buttonStartApp.Enabled = false;
            this.Text = windowtitle;
            labelSpeed.Text = string.Empty;
            bool checkboxStatus;
            try
            {
                checkboxStatus = Convert.ToBoolean(hyperSettings["autostart"].Value);
                patchversion = Convert.ToUInt32(hyperSettings["patchversion"].Value);
                hyperversion = Convert.ToUInt32(hyperSettings["hyperversion"].Value);
                webpatch = new Uri(hyperSettings["webpatch"].Value);
            }
            catch (Exception)
            {
                checkboxStatus = true;
                patchversion = 0;
                hyperversion = 0;
                webpatch = new Uri("");
            }
            checkBoxAutoStart.Checked = checkboxStatus;

            if (this.enableNotice == true)
                DownloadNotices();
            else
                TransformMini();

            InitPatchProcess();
        }

        private void DownloadNotices()
        {
            labelStatus.Text = "Downloading Notices.";
            try
            {
                notice = web2string(webpatch + "notice");
                textBoxNotice.Text = notice;
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
                patchlist = web2string(webpatch.AbsoluteUri + "patchlist");
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

                    if ((package.Name.Contains(".zip")) && package.Version > patchversion) //autoupdate files
                        filelist.Add(package);

                    else if (package.Name.Contains(".hyp") && package.Version > hyperversion) //autoupdate patcher
                        HyperbyteUpdate();
                }
            }
            return filelist;
        }

        private void FinishPatchProcess()
        {
            CleanDirectory(tempfolder);
            WriteConfigFile("patchversion", patchversion.ToString());
            this.Text = string.Format("[Completed] {0}", windowtitle);
            progressBar.Value = progressBar.Maximum;
            labelStatus.Text = "Patch Process Completed.";
            buttonStartApp.Enabled = true;
            if (checkBoxAutoStart.Checked)
                StartApplication();
        }

        private async void DownloadFiles(List<Package> package2download)
        {
            if (package2download != null)
            {
                labelStatus.Text = "Attempt to download files. Please wait.";
                tempfolder = Directory.CreateDirectory("temp");
                path2extract = Directory.CreateDirectory("tempext");
                patchfolder = Directory.GetParent(tempfolder.Name);
                CleanFiles(tempfolder);
                CleanFiles(path2extract);
                patcherDownloading = false;
                downloadCompleted = false;
                var count = 0;

                while (!downloadCompleted)
                {

                    foreach (Package package in package2download)
                    {
                        count++;
                        package.Localization = tempfolder.FullName + "\\" + package.Name;

                        while (patcherDownloading)
                            await Task.Delay(1000);

                        if (!package.Downloaded)
                        {
                            using (webclient = new WebClient())
                            {
                                sw.Start();
                                webclient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                                webclient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                                labelStatus.Text = "Fetching " + package.Name;
                                this.Text = string.Format("[{1}/{2}] {0}", windowtitle, count, package2download.Count);
                                try
                                {
                                    var fileaddress = new Uri(webpatch.AbsoluteUri + package.Name);
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
                    }

                    while (patcherDownloading)
                        await Task.Delay(1000);

                    downloadCompleted = true;
                    RecheckPackages(package2download);
                }
                FinishPatchProcess();
            }
            return;
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            labelSpeed.Text = string.Format("{0} kb/s", ((e.BytesReceived / 1024d) / sw.Elapsed.TotalSeconds).ToString("0.00")); //must test
            labelStatus.Text = string.Format("Downloading {0} [{1} MBs / {2} MBs ({3}%)]", pkgDownloading.Name, (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"), e.ProgressPercentage.ToString());
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            var failure = e.Error;

            if (failure == null)
            {
                sw.Reset();
                pkgDownloading.Downloaded = true;
                ExtractPackage(pkgDownloading);
                CleanDirectory(path2extract);
                patchversion = pkgDownloading.Version;
                WriteConfigFile("patchversion", patchversion.ToString());
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
            StringBuilder sb = new StringBuilder();
            byte[] buffer = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            string tempString = null;
            int count = 0;
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
                ZipFile.ExtractToDirectory(package.Localization, path2extract.Name);
                package.Extracted = true;
                ReplaceFile(path2extract, patchfolder);
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
                    var file2move = Path.Combine(patchfolder.FullName, sourcefile.Name);
                    File.Move(sourcefile.FullName, file2move);
                }
            }

        }

        private void RecheckPackages(List<Package> package2download)
        {
            labelStatus.Text = "Rechecking Packages.";
            foreach (Package pkg in package2download)
                if (pkg.Downloaded == false || pkg.Extracted == false)
                {
                    downloadCompleted = false;
                    return;
                }
        }

        private void CleanDirectory(string path)
        {
            labelStatus.Text = "Cleaning paths.";
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                if (Directory.Exists(path))
                    Directory.Delete(path);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "CleanFiles Error");
                Environment.Exit(0);
            }
        }

        private void CleanDirectory(DirectoryInfo directory)
        {
            labelStatus.Text = "Cleaning stuff.";
            try
            {
                foreach (FileInfo file in directory.GetFiles())
                {
                    if (File.Exists(file.FullName))
                        file.Delete();
                }
                if (Directory.Exists(directory.FullName))
                    Directory.Delete(directory.FullName);
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

        private void TransformMini()
        {
            panelNotice.Hide();
            panelMain.Location = new System.Drawing.Point(13, 13);
            Size = new System.Drawing.Size(500, 170);
        }

        private void buttonStartApp_Click(object sender, EventArgs e)
        {
            WriteConfigFile("autostart", checkBoxAutoStart.Checked.ToString());
            StartApplication();
        }

        private void buttonClosePatcher_Click(object sender, EventArgs e)
        {
            WriteConfigFile("autostart", checkBoxAutoStart.Checked.ToString());
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
                application.StartInfo.FileName = this.filename;
                application.StartInfo.Arguments = this.arguments;
                application.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open " + filename + "\nError: " + e.Message, "StartApplication Error");
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
