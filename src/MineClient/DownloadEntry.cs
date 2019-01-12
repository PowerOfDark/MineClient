using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MineClient
{
    public class DownloadEntry
    {
        public string SHA1;
        public string RemotePath;
        public string LocalPath;
        public DownloadEntry(string remote, string local, string sha)
        {
            this.SHA1 = sha;
            this.RemotePath = remote;
            this.LocalPath = local;
        }
    }

    public class ForgeDownloadEntry : DownloadEntry
    {
        public ForgeDownloadEntry(string remote, string local, string sha): base(remote, local, sha)
        {

        }
    }

    public class AssetDownloadEntry : DownloadEntry
    {
        public int Size { get; protected set; }
        public AssetDownloadEntry(string remote, string local, string sha, int size) : base(remote, local, sha)
        {
            this.Size = size;
        }
    }
}
