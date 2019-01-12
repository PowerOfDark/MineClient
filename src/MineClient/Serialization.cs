using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MineClient
{
    public static class Serialization
    {
        #region serialization
        [Obsolete("JSON switch", true)]
        public static FileDatabase Deserialize(this FileDatabase db, string file)
        {
            var serializer = new XmlSerializer(typeof(FileDatabase));
            FileDatabase temp = serializer.Deserialize(new StringReader(file)) as FileDatabase;
            db = temp;
            return db;
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        [Obsolete("JSON switch", true)]
        public static void Serialize(this FileDatabase creds, string path)
        {
            using (var writer = new System.IO.StreamWriter(path, false))
            {
                var serializer = new XmlSerializer(typeof(FileDatabase));
                serializer.Serialize(writer, creds);
                writer.Flush();
            }
        }
        public static void SerializeJson(this FileDatabase data, string path)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(FileDatabase));
           
            using (MemoryStream ms = new MemoryStream())
            {
                js.WriteObject(ms, data);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                using (var writer = new System.IO.StreamWriter(path, false))
                {
                    writer.Write(sr.ReadToEnd());
                }
            }
        }

        public static FileDatabase DeserializeJson(this FileDatabase db, string path)
        {
            string file = "";
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    file = sr.ReadToEnd();
                }
            }
            catch { }
            return DeserializeJsonString(db, file);
        }
        public static FileDatabase DeserializeJsonString(this FileDatabase db, string str)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(FileDatabase));
            byte[] file;
            file = Encoding.UTF8.GetBytes(str);
            using (MemoryStream ms = new MemoryStream(file))
                db = (FileDatabase)js.ReadObject(ms);
            return db;
        }
        [Obsolete("Use SHA1")]
        public static string MD5Checksum(this FileInfo f)
        {
            //return SHA1Checksum(f);
            using (var md5 = MD5.Create())
            {
                using (var stream = new BufferedStream(File.OpenRead(f.FullName), (f.Length < 1200000) ? (int)f.Length+1 : 1200000))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        public static string SHA1Checksum(this FileInfo f)
        {
            using (var sha = new SHA1Managed())
            {
                using (var stream = new BufferedStream(File.OpenRead(f.FullName), (f.Length < 1200000) ? (int)f.Length + 1 : 1200000))
                {
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
        #endregion
    }
}
