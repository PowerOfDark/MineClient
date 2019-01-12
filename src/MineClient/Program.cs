using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MineClient
{
    static class Program
    {
        public static RemoteRepository repo;
        public static string JAVA_HOME = null;
        private static string GetJavaInstallationPath()
        {
            try
            {
                var dirs = new DirectoryInfo(Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Java"));
                var jv = dirs.EnumerateDirectories().Where(t => File.Exists(Path.Combine(t.FullName, "bin", "javaw.exe"))).OrderByDescending(t => t.FullName.StartsWith("jre") ? 1 : 0).ThenByDescending(t => t.Name).FirstOrDefault();
                if (jv != null)
                {
                    return jv.FullName;
                }

                string environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (!string.IsNullOrEmpty(environmentPath))
                {
                    return environmentPath;
                }

                try
                {
                    string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
                    using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                    {
                        string currentVersion = rk.GetValue("CurrentVersion").ToString();
                        using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                        {
                            return key.GetValue("JavaHome").ToString();
                        }
                    }
                }
                catch
                {
                    string javaKey = "SOFTWARE\\JavaSoft\\Java Development Kit\\";
                    using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                    {
                        string currentVersion = rk.GetValue("CurrentVersion").ToString();
                        using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                        {
                            return key.GetValue("JavaHome").ToString();
                        }
                    }

                }
            }
            catch
            {

                return null;
            }
        }

        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;

            string MAINPATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PowerOfDark\\MineClient\\.update\\";
            
            string sha1 = "";
            try
            {
                //Coming from an old release
                string oldRel = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PowerOfDark\\MineClient\\.game\\";
                if (File.Exists(oldRel + "path.txt"))
                {
                    try
                    {
                        Directory.Delete(oldRel, true);
                    }
                    catch
                    {

                    }
                    try
                    {
                        Directory.Delete(oldRel, true);
                    }
                    catch
                    {

                    }
                    //MessageBox.Show("Due to an update, all old instances have been purged.");
                }

                repo = new RemoteRepository(new object[] { "https://staszic.net/~powerofdark/", "minecraft/" });
                //Console.WriteLine("Using {0}", (repo.IsDropbox) ? "Dropbox" : "http");
                Console.WriteLine(Form1.ROOT);
                string utils = Path.Combine(Form1.MAINPATH, ".utils");
                if (!Directory.Exists(utils))
                    Directory.CreateDirectory(utils);

                File.WriteAllBytes(Path.Combine(utils, "forge-extract.jar"), Properties.Resources.forge_extract);
                sha1 = repo.GetString(".update/sha1");

            }
            catch { MessageBox.Show("Cannot connect to server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); Environment.Exit(-1); }
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                string oldName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".exe";
                string updatedName = oldName + ".new";//.exe.new
                
                string localsha = new FileInfo(Assembly.GetEntryAssembly().Location).SHA1Checksum();
                if ((localsha != sha1 && sha1.Length > 5))
                {
                    try
                    {
                        if (Directory.Exists(MAINPATH))
                            Directory.Delete(MAINPATH, true);
                        Directory.CreateDirectory(MAINPATH);
                        repo.DownloadFile(".update/MineClient.exe", MAINPATH + updatedName);
                        using (StreamWriter sw = new StreamWriter(MAINPATH + "path"))
                        {
                            sw.Write(Assembly.GetEntryAssembly().Location);
                        }
                        Process p = new Process();
                        p.StartInfo = new ProcessStartInfo() { UseShellExecute = false, WorkingDirectory = MAINPATH, FileName = Path.Combine(MAINPATH, updatedName) };
                        p.Start();
                        Environment.Exit(1337);
                    }
                    catch { MessageBox.Show("Cannot update", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); Environment.Exit(-1); }
                }

                if (new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Name == new DirectoryInfo(MAINPATH).Name && Path.GetFileName(Assembly.GetEntryAssembly().Location).EndsWith(".new") && new DirectoryInfo(MAINPATH).EnumerateFiles("path").Count() == 1)//if this is the .new file
                {
                    using (StreamReader sr = new StreamReader(MAINPATH + "path"))
                    {
                        oldName = sr.ReadToEnd();
                        sr.Close();
                    }
                    var sd = WaitForFile(oldName, FileMode.Open, FileAccess.ReadWrite);
                    sd.Close();
                    File.Copy(new FileInfo(Assembly.GetEntryAssembly().Location).Name, oldName, true);
                    File.Move(MAINPATH + "path", MAINPATH + "done");
                    new Process() { StartInfo = new ProcessStartInfo() { FileName = oldName, UseShellExecute = false } }.Start();
                    Environment.Exit(1337);
                }

                if (localsha == sha1 && File.Exists(Path.Combine(MAINPATH, "done")))// && !Path.GetFileName(Assembly.GetEntryAssembly().Location).EndsWith(".new"))//if this is the replaced file
                {
                    Thread.Sleep(500);
                    updatedName = Path.Combine(MAINPATH, oldName + ".new");
                    var sd = WaitForFile(updatedName, FileMode.Open, FileAccess.ReadWrite);
                    sd.Close();
                    Thread.Sleep(500);
                }
            }
            try
            {
                Directory.Delete(MAINPATH, true);
            }
            catch { }

            JAVA_HOME = GetJavaInstallationPath();
            //MessageBox.Show($"Kurwa\n\tJAVA_HOME={JAVA_HOME}");

            if (JAVA_HOME == null || JAVA_HOME.Length < 2 || !File.Exists(Path.Combine(JAVA_HOME, "bin", "java.exe")))
            {
                MessageBox.Show("Please install Java to continue.", "No Java runtime found");
                Environment.Exit(-1);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Exception: {(e.ExceptionObject as Exception).Message}\n{(e.ExceptionObject as Exception).StackTrace}");
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
        }

        static FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access)
        {
            while (true)
            {
                try
                {
                    FileStream fs = new FileStream(fullPath, mode, access);

                    fs.ReadByte();
                    fs.Seek(0, SeekOrigin.Begin);

                    return fs;
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
            }
        }

    }
}
