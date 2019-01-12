using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MineClient
{
    public class VersionDownloader
    {
        public string Root { get; protected set; }
        public string Version { get; protected set; }
        public List<JObject> Manifests { get; protected set; }
        public JObject Manifest { get; protected set; }
        public bool IsInherited { get; protected set; }
        public string ActualVersion { get; protected set; }

        protected string jarPath = "";
        protected List<string> libraryList = new List<string>();

        public const string LIB_PATH = "https://libraries.minecraft.net/";
        public const string ASSET_PATH = "http://resources.download.minecraft.net/";


        public class OS
        {
            [JsonProperty]
            public string name;
        }

        public class Rule
        {
            [JsonProperty]
            public string action;
            [JsonProperty]
            public OS os;
        }

        public VersionDownloader(string localRoot, string version)
        {
            this.Manifests = new List<JObject>();
            this.Root = localRoot;
            this.Version = version;
            
        }

        public void GetManifests()
        {
            JObject m = null;
            string target = Version;
            do
            {
                if(m != null)
                {
                    target = (string)m.GetValue("inheritsFrom");
                }
                var dest = Path.Combine(Root, "versions", target);
                if (!Directory.Exists(dest))
                    Directory.CreateDirectory(dest);
                string targetPath = Path.Combine(dest, target + ".json");
                Program.repo.DownloadFile($"versions/{target}/{target}.json", targetPath);
                using (StreamReader sr = new StreamReader(targetPath))
                {
                    string content = sr.ReadToEnd();
                    m = JObject.Parse(content);
                }
                 
                Manifests.Add(m);
            }
            while (m.TryGetValue("inheritsFrom", out JToken _));
            IsInherited = Manifests.Count > 1;
            Manifest = Manifests.Last();
            ActualVersion = (string)Manifest.GetValue("id");
            for(int i = (Manifests.Count - 2); i >= 0; i--)
            {
                Manifest.Merge(Manifests[i], new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Concat });
            }
        }
        public string ResolveLibraryName(string n)
        {
            string[] s = n.Split(':');
            string s1 = s[0];
            string res = s1.Replace(".", "/") + $"/{s[1]}/{s[2]}/{s[1]}-{s[2]}.jar";
            return res;
        }
        public (List<DownloadEntry> NormalLibraries, List<DownloadEntry> ForgeLibraries, List<string> LocalNatives) GetDownloadList()
        {
            libraryList.Clear();
            dynamic m = Manifest as dynamic;
            List<DownloadEntry> list = new List<DownloadEntry>();
            List<DownloadEntry> forge = new List<DownloadEntry>();
            List<string> localNatives = new List<string>();
            string jarpath = Path.Combine(Root, "versions", ActualVersion, ActualVersion + ".jar");
            jarPath = jarpath;
            if (!File.Exists(jarpath) || new FileInfo(jarpath).SHA1Checksum() != (string)m.downloads.client.sha1)
                list.Add(new DownloadEntry((string)m.downloads.client.url, jarpath, (string)m.downloads.client.sha1));

            string libpath = Path.Combine(Root, "libraries");
            foreach(var dl in Manifest.GetValue("libraries").ToList())
            {
                string name = ResolveLibraryName(dl.Value<string>("name"));
                var dict = (Dictionary<string, object>)dl.ToObject(typeof(Dictionary<string, object>));
                bool IsAllowed = true;
                if(dict.ContainsKey("rules"))
                {
                    var rules = (List<Rule>)(dict["rules"] as JArray).ToObject(typeof(List<Rule>));
                    HashSet<string> allowedOs = new HashSet<string>();
                    HashSet<string> disallowedOs = new HashSet<string>();
                    bool allAllowed = false;
                    foreach(var r in rules)
                    {
                        if (r.action == "allow" && r.os == null) allAllowed = true;
                        else if (r.action == "allow") allowedOs.Add(r.os.name);
                        else if (r.action == "disallow") disallowedOs.Add(r.os.name);
                    }
                    IsAllowed = !disallowedOs.Contains("windows") && (allAllowed || allowedOs.Contains("windows"));
                }
                if (!IsAllowed) continue;
                bool isNative = dict.ContainsKey("natives");
                if(isNative)
                {
                    name = name.Substring(0, name.Length - 4) + "-" + ((string)((dynamic)dict["natives"]).windows).Replace("${arch}", (IntPtr.Size * 8).ToString()) + ".jar";
                }
                string jar = Path.Combine(libpath, name);
                if (isNative)
                {
                    localNatives.Add(jar);
                }
                else
                {
                    libraryList.Add(jar);
                }
                bool missing = (!File.Exists(jar) || !File.Exists(jar + ".sha"));
                FileInfo info = new FileInfo(jar);
                if (!missing)
                {
                    string sh;
                    using (StreamReader sr = new StreamReader(jar + ".sha"))
                    {
                        sh = sr.ReadToEnd();
                    }
                    if (sh != info.SHA1Checksum()) missing = true;
                }
                if(missing)
                {
                    
                    bool isForge = dict.ContainsKey("url");
                    if (!isForge)
                    {
                        info.Directory.Create();
                        string sha;
                        using (WebClient wc = new WebClient())
                        {
                            wc.DownloadFile(LIB_PATH + name + ".sha1", jar + ".sha");
                        }
                        using (StreamReader sr = new StreamReader(jar + ".sha"))
                        {
                            sha = sr.ReadToEnd();
                        }
                        list.Add(new DownloadEntry(LIB_PATH + name, jar, sha));
                    }
                    else
                    {
                        if(name.Split('/').Last().StartsWith("forge-"))
                        {
                            name = name.Substring(0, name.Length - 4) + "-universal.jar";
                            forge.Add(new ForgeDownloadEntry(dict["url"] + name, jar, "FORGE"));
                        }
                        else
                            forge.Add(new ForgeDownloadEntry(dict["url"] + name + ".pack.xz", jar, "FORGE"));
                    }
                }
            }

            return (list, forge, localNatives);
        }

        public class Item
        {
            [JsonProperty]
            public int size;
            [JsonProperty]
            public string hash;

            public Item() { }
        }

        public (List<DownloadEntry> DownloadList, long DownloadSize) GetAssetDownloadList()
        {
            List<DownloadEntry> list = new List<DownloadEntry>();
            string ver = (string)Manifest.GetValue("assets");
            string path = Path.Combine(Root, "assets", "indexes", ver + ".json");
            FileInfo info = new FileInfo(path);
            if (!info.Directory.Exists)
                info.Directory.Create();
            using (WebClient wc = new WebClient())
                wc.DownloadFile($"https://s3.amazonaws.com/Minecraft.Download/indexes/{ver}.json", path);
            JObject assetIndex;
            using (StreamReader sr = new StreamReader(path))
            {
                assetIndex = JObject.Parse(sr.ReadToEnd());
            }
            string objectsDir = Path.Combine(Root, "assets", "objects");
            if (!Directory.Exists(objectsDir))
                Directory.CreateDirectory(objectsDir);
            var dict = (Dictionary<string, Item>) assetIndex.GetValue("objects").ToObject(typeof(Dictionary<string, Item>));
            long downloadSize = 0;
            foreach(var kv in dict)
            {
                string objPath = $"{kv.Value.hash.Substring(0, 2)}/{kv.Value.hash}";
                FileInfo objInfo = new FileInfo(Path.Combine(objectsDir, objPath));
                if(!objInfo.Exists)
                {
                    list.Add(new AssetDownloadEntry(ASSET_PATH + objPath, objInfo.FullName, kv.Value.hash, kv.Value.size));
                    downloadSize += kv.Value.size;
                }
            }

            return (list, downloadSize);
        }

        public List<string> GetListOfLoadLibraries()
        {
            return this.libraryList;
        }

        public string GetJarPath()
        {
            return jarPath;
        }


    }
}
