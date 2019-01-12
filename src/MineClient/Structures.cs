using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MineClient
{
    [DataContract]
    public class FileDatabase
    {
        [DataMember]
        public List<FileEntry> Entries;

        public FileDatabase(List<FileEntry> entries)
        {
            this.Entries = new List<FileEntry>(entries);
        }

        internal FileDatabase() { this.Entries = new List<FileEntry>(); }
    }

    public class FileEntry
    {
        [DataMember]
        public string Path;
        [DataMember]
        public string MD5;
        [DataMember]
        public bool Editable;
        [DataMember]
        public ulong Size;
        public FileEntry(string Path, string MD5, ulong Size, bool Editable = false)
        {
            this.Path = Path;
            this.MD5 = MD5;
            this.Editable = Editable;
            this.Size = Size;
        }

        internal FileEntry() { }
    }
}
