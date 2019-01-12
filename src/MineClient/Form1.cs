using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace MineClient
{
    public partial class Form1 : Form
    {
        IEnumerable<FileInfo> localFiles;
        //FileDatabase local = new FileDatabase();
        public const string BASE_TITLE = "MineClient";
        public const int BUILD = 190106;
        public const string ROOT = "/minecraft/";
        public static string MAINPATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PowerOfDark\\MineClient\\.game";
        public static string Utility_ForgeExtract = Path.Combine(MAINPATH, ".utils", "forge-extract.jar");
        ConcurrentBag<FileEntry> ToDownload;
        int processed = 0, toProcess = 0;
        string status;
        string status1;
        string status2;
        DateTime DlStart;
        long DlTransferred = 0;
        long DlToDownload = 0;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int operation = 0;
        ConcurrentBag<FileEntry> localEntries;
        ConcurrentBag<FileEntry> remoteEntries;
        long DlTime = 0;
        long DlTransferred_last = 0;
        System.Windows.Forms.Timer gc = new System.Windows.Forms.Timer() { };
        List<string> AvailableProfiles = new List<string>();
        string CurrentProfile = "";
        VersionDownloader VersionDownloader;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        public Form1()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Shown += Form1_Shown;
            
            
            InitializeComponent();
            //this.progressBar1.Configure(Properties.Resources.progress);
            //this.glowRenderer.Init(Properties.Resources.glow, 10);
            gc.Tick += Gc_Tick;
            gc.Interval = 30000;
            gc.Start();
            if (!Directory.Exists(MAINPATH))
            {
                Directory.CreateDirectory(MAINPATH);
            }

        }

        private void Gc_Tick(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        void StartWorker()
        {
            Thread t = new Thread(() =>
            {
                progressBar1.Invoke((Action)delegate { progressBar1.Show(); startBtn.Hide(); usernameInput.Hide(); uninstallBtn.Hide(); profileComboBox.Hide(); });

                operation = 0;
                status1 = "Preparing";
                VersionDownloader = new VersionDownloader(Form1.MAINPATH, CurrentProfile);
                VersionDownloader.GetManifests();
                
                var tdl = VersionDownloader.GetDownloadList();
                var ass = VersionDownloader.GetAssetDownloadList();
                tdl.NormalLibraries.AddRange(tdl.ForgeLibraries);
                ConcurrentBag<DownloadEntry> todown = new ConcurrentBag<DownloadEntry>(tdl.NormalLibraries);
                operation = 1;
                DownloadRemote("Libraries", todown); operation++;
               
                
                todown = new ConcurrentBag<DownloadEntry>(ass.DownloadList);
                DownloadRemote("Assets", todown, ass.DownloadSize); operation++;

                IndexLocalFiles(); operation++;

                IndexRemoteFiles(); operation++;

                GenerateList(); operation++;

                Gc_Tick(null, null);

                SyncFiles(); operation++;

                status1 = "Cleaning";
                CleanUp(); operation++;
                string nativesPath = Path.Combine(MAINPATH, "versions", CurrentProfile, "natives");
                if(Directory.Exists(nativesPath))
                    Directory.Delete(nativesPath, true);
                Directory.CreateDirectory(nativesPath);
                byte[] buf = new byte[4096];
                foreach (var n in tdl.LocalNatives)
                {
                    using (ICSharpCode.SharpZipLib.Zip.ZipFile z = new ICSharpCode.SharpZipLib.Zip.ZipFile(n))
                    {
                        foreach(ICSharpCode.SharpZipLib.Zip.ZipEntry entry in z)
                        {
                            if(entry.Name.EndsWith(".dll"))
                            {
                                using (FileStream fw = File.OpenWrite(Path.Combine(nativesPath, entry.Name)))
                                {
                                    
                                    Stream zipStream = z.GetInputStream(entry);
                                    StreamUtils.Copy(zipStream, fw, buf);
                                }
                            }
                        }
                    }
                }
                processDirectory(Path.Combine(MAINPATH, CurrentProfile));
                Program.repo.DownloadFile("launcher.bat", Path.Combine(MAINPATH, "launcher.bat"));
                operation++;
                status = "";
                Gc_Tick(null, null);
                progressBar1.Invoke((Action)delegate
                {
                    progressBar1.Hide();
                    //this.Controls.RemoveByKey("progressBar1"); progressBar1.Dispose(); progressBar1 = null;
                });
                string username = "Player";
                try
                {
                    using (StreamReader r = new StreamReader(MAINPATH + "\\.name"))
                    {
                        string temp = r.ReadLine();
                        if (temp.Trim().Length > 0)
                            username = temp;
                    }
                }
                catch { }
                this.Invoke((Action)delegate { usernameInput.Text = username; startBtn.Show(); usernameInput.Show(); uninstallBtn.Show(); profileComboBox.Show(); });

                //sync the animation after patching so it looks awesome
                if (glowRenderer.GlowIteration == -1)
                {
                    glowRenderer.Animate();
                }


            })
            { IsBackground = true };
            t.Start();
        }

        void Form1_Shown(object sender, EventArgs e)
        {

            timer.Tick += timer_Tick;
            timer.Interval = 350;
            timer.Start();
            timer.Enabled = true;
            
            AvailableProfiles = (List<string>)JArray.Parse(Program.repo.GetString("profiles.json")).ToObject(typeof(List<string>));
            this.profileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;
            this.profileComboBox.Items.Clear();
            this.profileComboBox.Items.Add("Select a profile");
            this.profileComboBox.SelectedIndex = 0;
            this.profileComboBox.Items.AddRange(AvailableProfiles.ToArray());
            //StartWorker();

        }

        private void ProfileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (profileComboBox.SelectedIndex > 0)
            {
                profileComboBox.Hide();
                CurrentProfile = AvailableProfiles[profileComboBox.SelectedIndex - 1];
                StartWorker();
            }

        }

        private void CleanUp()
        {
            //string[] keep = new string[] { "saves", "screenshots", "stats", "config", ".name", "journeymap" };
            string[] remove = new string[] { "mods" };
            ConcurrentBag<FileEntry> toRemove = new ConcurrentBag<FileEntry>();
            Thread t = new Thread(() => { Parallel.ForEach(localEntries, (f) => { if (!remoteEntries.Any(a => a.Path == f.Path)) { toRemove.Add(f); } }); }) { IsBackground = true };
            t.Start();
            t.Join();

            //todo
            //Actual cleanup, since deleting files doesn't seem right...
            foreach (FileEntry f in toRemove)
            {
                if (!remove.Contains(f.Path.Replace("/", "\\").Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[0]))
                    continue;
                
                Console.WriteLine($"Deleted {f.Path}");
                File.Delete(MAINPATH + $"\\{CurrentProfile}\\" + f.Path);
            }

        }

        void IndexLocalFiles()
        {
            var dirInfo = new DirectoryInfo(Path.Combine(MAINPATH, CurrentProfile));
            if (!dirInfo.Exists) dirInfo.Create();
            localFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            ConcurrentBag<FileInfo> localF = new ConcurrentBag<FileInfo>(localFiles);
            toProcess = localFiles.Count();
            localEntries = new ConcurrentBag<FileEntry>();
            status = "Indexing local files";
            Thread t = new Thread(() =>
            {
                Parallel.ForEach(localF, (f) => { localEntries.Add(new FileEntry(f.FullName.Replace(MAINPATH + $"\\{CurrentProfile}\\", "").TrimStart(new[] {'\\' }), f.SHA1Checksum(), (ulong)f.Length)); Interlocked.Increment(ref processed); });
            })
            { IsBackground = true };
            t.Start();
            t.Join();
            status = "";
            processed = toProcess = 0;
        }

        void IndexRemoteFiles()
        {
            toProcess = 1;
            status = "Indexing remote files";
            string xml = "";
            Thread t = new Thread(() =>
            {
                //using (WebClient wc = new WebClient())
                //{

                //    xml = wc.DownloadString(ROOT + "release.xml");
                //}
                //string tes = Encoding.UTF8.GetString(_client.GetFile(ROOT + "options.txt"));
                try
                {
                    xml = Program.repo.GetString($"{CurrentProfile}/release.json");
                }
                catch
                {
                    xml = "{}";
                }
            })
            { IsBackground = true };
            t.Start();
            t.Join();
            if (xml?.StartsWith("{}") ?? true)
                remoteEntries = new ConcurrentBag<FileEntry>();
            else
                remoteEntries = new ConcurrentBag<FileEntry>(new FileDatabase().DeserializeJsonString(xml).Entries);
            foreach(var e in remoteEntries)
            {
                e.Path = e.Path.TrimStart(new[] { '\\' });
            }
            status = "";
            processed = toProcess = 0;
        }

        void GenerateList()
        {
            toProcess = remoteEntries.Count;
            status = "Calculating differences";
            ToDownload = new ConcurrentBag<FileEntry>();
            Thread t = new Thread(() =>
            {
                Parallel.ForEach(new ConcurrentBag<FileEntry>(remoteEntries), (f) =>
                {
                    if ((!localEntries.Any(s => s.Path == f.Path)
                        || (localEntries.First(a => a.Path == f.Path).MD5 != f.MD5 && !f.Editable)))
                    {
                        if (f != null)
                        {
                            ToDownload.Add(f);
                        }
                    }
                    Interlocked.Increment(ref processed);
                });

            })
            { IsBackground = true };
            t.Start();
            t.Join();
            status = "";
            processed = toProcess = 0;
        }
        string assetPath(string file)
        {
            string f = Path.GetFileName(file);
            if (!f.EndsWith(".json"))
                return "http://resources.download.minecraft.net/" + f.Substring(0, 2) + "/" + f;
            else
                return "https://s3.amazonaws.com/Minecraft.Download/indexes/" + f;
        }

        void DownloadRemote(string label, ConcurrentBag<DownloadEntry> downloadEntries, long size = -1)
        {
            toProcess = downloadEntries.Count;
            status1 = label;
            DlStart = DateTime.Now;
            DlTransferred = 0;
            DlTime = 0;
            DlToDownload = (size == -1 ? downloadEntries.Count : size);//ToDownload.Sum(temp => (int)temp.Size);
            ConcurrentBag<DownloadEntry> toRedownload = new ConcurrentBag<DownloadEntry>();
            Color c = new Color();
            Thread t = new Thread(() =>
            {
                Parallel.ForEach(downloadEntries, new ParallelOptions() { MaxDegreeOfParallelism = 1*(label=="Assets"?2:1) }, (fe) =>
                {
                    try
                    {
                        FileInfo info = new FileInfo(fe.LocalPath);
                        status = info.Name;
                        if (info.Exists) info.Delete();
                        if (!info.Directory.Exists)
                            info.Directory.Create();
                        DateTime dlstarted = DateTime.Now;
                        bool forgeNotPacked = false;
                        string tmpLocalName = fe.LocalPath + ".tmp";
                        info = new FileInfo(tmpLocalName);
                        try
                        {
                            using (WebClient wc = new WebClient())
                                wc.DownloadFile(fe.RemotePath, tmpLocalName);
                        }
                        catch(Exception exc)
                        {
                            if (fe is ForgeDownloadEntry)
                            {
                                forgeNotPacked = true;
                                //can't change fe?
                                fe.RemotePath = fe.RemotePath.Substring(0, fe.RemotePath.Length - (".pack.xz").Length);
                                using (WebClient wc = new WebClient())
                                    wc.DownloadFile(fe.RemotePath, tmpLocalName);
                            }
                            else
                                throw exc;
                        }
                        Interlocked.Add(ref DlTime, (int)(DateTime.Now - dlstarted).TotalMilliseconds);
                        info.Refresh();
                        Interlocked.Add(ref DlTransferred, info.Length);
                        status2 = "$DOWNLOAD$";
                        Interlocked.Increment(ref processed);

                        
                        if(fe is ForgeDownloadEntry)
                        {
                            if (fe.RemotePath.EndsWith(".pack.xz") && !forgeNotPacked)
                            {
                                // oh boy..
                                var exe = new ProcessUtilities.Executable();
                                exe.Arguments = $"-jar \"{Utility_ForgeExtract}\" \"{tmpLocalName}\" \"{tmpLocalName}.ext\"";
                                exe.ProgramFileName = Path.Combine(Program.JAVA_HOME, "bin", "java.exe");
                                exe.Run();
                                File.Delete(tmpLocalName);
                                File.Move(tmpLocalName + ".ext", tmpLocalName);
                            }
                        }
                        string actualSha;
                        if (!(fe is AssetDownloadEntry))
                        {
                            using (StreamWriter fw = new StreamWriter(fe.LocalPath + ".sha", false))
                            {
                                fw.Write(actualSha = ((fe.SHA1.Length < 40) ? info.SHA1Checksum() : fe.SHA1));
                            }
                        }
                        if(fe.SHA1.Length == 40 && fe.SHA1 != info.SHA1Checksum())
                        {
                            info.Delete();
                            throw new Exception("Invalid checksum");
                        }
                        File.Move(tmpLocalName, fe.LocalPath);



                        Thread.Sleep(5);
                    }
                    catch (Exception ex) { toRedownload.Add(fe); Console.WriteLine("Retrying {1} ({0})", ex.Message, fe.RemotePath); }
                });


            })
            { IsBackground = true };
            t.Start();
            t.Join();

            status2 = status1 = status = "";
            processed = toProcess = 0;
            DlTime = 0;
            DlToDownload = 0;
            if (toRedownload.Count > 0)
            {
                //ToDownload = toRedownload;
                DownloadRemote(label, toRedownload);
            }
        }

        void SyncFiles(string label = "Patching")
        {
            toProcess = ToDownload.Count;
            status1 = label;
            DlStart = DateTime.Now;
            DlTransferred = 0;
            DlTime = 0;
            DlToDownload = ToDownload.Sum(temp => (int)temp.Size);
            ConcurrentBag<FileEntry> toRedownload = new ConcurrentBag<FileEntry>();
            Color c = new Color();
            Thread t = new Thread(() =>
            {
                Parallel.ForEach(ToDownload, new ParallelOptions() {MaxDegreeOfParallelism=6}, (fe) =>
                {
                    try
                    {
                        string currentPath = MAINPATH + $"\\{CurrentProfile}\\" + fe.Path;
                        
                        status = new FileInfo(fe.Path).Name;
                        if (File.Exists(currentPath))
                            File.Delete(currentPath);
                        string tmpLocalName = currentPath + ".tmp";
                        
                        //string s = ROOT + fe.Path.Replace('\\', '/');
                        FileInfo info = new FileInfo(tmpLocalName);
                        var dir = info.Directory;
                        if (!dir.Exists)
                            dir.Create();
                        DateTime dlstarted = DateTime.Now;
                        
                        Program.repo.DownloadFile(CurrentProfile + "/" + fe.Path.Replace('\\', '/'), tmpLocalName);
                        Interlocked.Add(ref DlTime, (int)(DateTime.Now - dlstarted).TotalMilliseconds);
                        info.Refresh();
                        Interlocked.Add(ref DlTransferred, info.Length);
                        status2 = "$DOWNLOAD$";
                        Interlocked.Increment(ref processed);
                        var sha = info.SHA1Checksum();
                        if(sha != fe.MD5)
                        {
                            info.Delete();
                            throw new Exception("Invalid checksum");
                        }
                        File.Move(tmpLocalName, currentPath);


                        Thread.Sleep(5);
                    }
                    catch (Exception ex) { toRedownload.Add(fe); Console.WriteLine("Retrying {1} ({0})", ex.Message, fe.Path); }
                });


            })
            { IsBackground = true };
            t.Start();
            t.Join();

            status2 = status1 = status = "";
            processed = toProcess = 0;
            DlTime = 0;
            DlToDownload = 0;
            if (toRedownload.Count > 0)
            {
                ToDownload = toRedownload;
                SyncFiles(label);
            }
        }

        private static void processDirectory(string startLocation)
        {

            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    try
                    {
                        Directory.Delete(directory, false);
                    }
                    catch { }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            glowRenderer.Configure();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                this.Invoke((Action)delegate
                {
                    if (progressBar1 != null)
                    {
                        SuspendUpdates(progressBar1);
                        progressBar1.MainText = status;

                        string oldstatus1 = progressBar1.StatusText;
                        progressBar1.StatusText = status1;
                        progressBar1.LeftText = operation.ToString();
                        if (status2 == "$DOWNLOAD$")
                        {
                            if (DlTransferred_last != DlTransferred)
                            {
                                string units = "KB/s";
                                double speed = ((ulong)DlTransferred / ((DateTime.Now - DlStart).TotalSeconds)) / 1024;
                                if (speed >= 1024)
                                {
                                    speed /= 1024;
                                    units = "MB/s";//dont think we need more than that... as of 2015
                                }
                                string speedstr = (units.Equals("KB/s") ? ((int)Math.Round(speed)).ToString() : speed.ToString("0.00")) + units;
                                progressBar1.StatusText += "\n" + speedstr;
                                this.Text = BASE_TITLE + " :: " + ((DlTransferred / 1024 / 1024)) + "/" + DlToDownload / 1024 / 1024 + "MB :: " + speedstr;
                            }
                            else
                                progressBar1.StatusText = oldstatus1;

                            DlTransferred_last = DlTransferred;
                        }
                        else
                        {
                            this.Text = BASE_TITLE;
                            progressBar1.StatusText += "\n" + status2;
                        }
                        //}
                        if (toProcess > 0)
                        {
                            progressBar1.MainText += string.Format(" ({0}/{1})", processed, toProcess);
                            progressBar1.FillDegree = (int)Math.Round(((double)processed / (double)toProcess) * 100);
                        }
                        else
                            progressBar1.FillDegree = 0;
                        ResumeUpdates(progressBar1);
                        progressBar1.Invalidate();
                    }
                    else
                    {
                        this.Text = BASE_TITLE + " " + BUILD;
                        timer.Stop();
                    }
                });
            }
            catch { }

        }

        public void SuspendUpdates(Harr.HarrProgressBar c)
        {
            SendMessage(c.Handle, 0XB, IntPtr.Zero, IntPtr.Zero);
        }

        public void ResumeUpdates(Harr.HarrProgressBar c)
        {
            SendMessage(c.Handle, 0XB, new IntPtr(1), IntPtr.Zero);
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (usernameInput.Text.Trim().Length > 0 && usernameInput.Text.Trim().Length < 17)
            {
                GenerateRunString(usernameInput.Text.Trim());
                using (StreamWriter sw = new StreamWriter(MAINPATH + "\\.name"))
                    sw.WriteLine(usernameInput.Text.ToString());
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo { FileName = MAINPATH + "\\launch.bat", CreateNoWindow = false, WorkingDirectory = MAINPATH, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden };
                p.Start();
                glowRenderer.AnimationTimer.Stop();
                this.Hide();
                DateTime starttime = DateTime.Now;
                p.WaitForExit();
                TimeSpan time = DateTime.Now - starttime;
                this.Show();
                this.Invalidate();
                string msg = "";
                if (time.TotalSeconds < 4)
                {
                    msg = "Hmmm, looks like you're having some issues, aren't you?\nTry (re)installing Java - that's solution to 99% of problems";
                }
                else if (time.TotalSeconds < 8)
                {
                    msg = "Seems like your PC might have a problem running this version of Minecraft. Try updating your drivers, or having at least 1GB of available RAM";
                }
                if (msg.Length > 0)
                    MessageBox.Show(msg, "Error detected", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);


                glowRenderer.Animate();
                glowRenderer.AnimationTimer.Start();
                //File.Delete(MAINPATH + "\\launch.bat");

            }
            else { MessageBox.Show("Invalid user name"); }
        }

        public string GenerateRunString(string username)
        {
            string content = "";
            using (StreamReader sr = new StreamReader(MAINPATH + "\\launcher.bat"))
            {
                content = sr.ReadToEnd().Replace("PAUSE", "").Replace("${java_path}", $"{Program.JAVA_HOME}\\bin\\javaw.exe");
            }
            string javaArguments = "";
            var manifestDict = ((Dictionary<string, object>)VersionDownloader.Manifest.ToObject(typeof(Dictionary<string, object>)));
            if (manifestDict.ContainsKey("javaArguments"))
            {
                javaArguments += manifestDict["javaArguments"];
            }
            content = content.Replace("REM REM REM BEFORE_LAUNCH REM REM REM", $"cd %lpath%\\{CurrentProfile}").Replace("REM REM REM JAVA_ARGUMENTS REM REM REM", javaArguments);
            //...
            string cmdline = "";

            //assets
            cmdline += $"-Djava.library.path=%lpath%\\versions\\{CurrentProfile}\\natives";

            //libs
            string libstring = "";
            foreach(var str in VersionDownloader.GetListOfLoadLibraries())
            {
                libstring += "%lpath%" + str.Substring((str.LastIndexOf("\\libraries\\"))).Replace('/','\\') + ";";
            }
            libstring += $@"%lpath%\versions\{VersionDownloader.ActualVersion}\{VersionDownloader.ActualVersion}.jar;";
            cmdline += $" -cp {libstring.Substring(0, libstring.Length-1)}";

            //rest
            string readArgs = (string)VersionDownloader.Manifest.GetValue("minecraftArguments");
            readArgs = readArgs.Replace("${auth_player_name}", username).Replace("${version_name}", CurrentProfile)
                .Replace("${game_directory}", Path.Combine(MAINPATH, CurrentProfile))
                .Replace("${assets_root}", "%lpath%\\assets")
                .Replace("${assets_index_name}", (string) VersionDownloader.Manifest.GetValue("assets"))
                .Replace("${auth_uuid}", "a620d469cda0365eaa1c3cc17361975a").Replace("${auth_access_token}", "\" \"")
                .Replace("${user_type}", username).Replace("${version_type}", "PowerOfDark/Enjoy!")
                .Replace("${user_properties}", "{}");
            string otherFlags = "";
            string main = (string)VersionDownloader.Manifest.GetValue("mainClass");
            cmdline += " " + main + " " + otherFlags + " " + readArgs + " " + "--width 1280 --height 720";
            content = content.Replace("REM REM REM ARGUMENTS REM REM REM", cmdline);
            using (StreamWriter sw = new StreamWriter(MAINPATH + "\\launch.bat", false))
            {
                sw.Write(content);
            }
            return content;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult r = MessageBox.Show("Are you sure you want to uninstall MineClient?\nYou'll lose your entire instance!", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                startBtn.Enabled = false;
                uninstallBtn.Enabled = false;
                uninstallBtn.Text = "Uninstalling...";
                Task.Factory.StartNew(() =>
                {
                    Directory.Delete(MAINPATH, true);
                }).Wait();
                uninstallBtn.Text = "done";
                MessageBox.Show("Uninstallation completed successfully. Cya!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
        }

        private void usernameInput_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
