using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MineClient
{
    public class RemoteRepository
    {
        /// <summary>
        /// WebClient or DropNetClient
        /// </summary>
        private object _repository;
        //public bool IsDropbox { get { return _http == null || _http.Length == 0; } }

        /// <summary>
        /// Root directory of the repository
        /// </summary>
        public string BaseAddress = "";
        private string _http;
        public RemoteRepository(params object[][] repos)
        {
            foreach (object[] _repo in repos)
            {
                var repo = _repo[0];
                this.BaseAddress = _repo[1].ToString();
                Console.WriteLine(repo.GetType().ToString());
                /*if (repo.GetType() == typeof(DropNetClient))
                {
                    DropNetClient c = repo as DropNetClient;

                    try
                    {
                        if (Encoding.UTF8.GetString(c.GetFile(".status")).Contains("online"))
                        {
                            if (Encoding.UTF8.GetString(c.GetFile($"{BaseAddress}/.status")).Contains("online"))
                            {
                                _repository = c;
                                break;
                            }
                        }

                    }
                    catch { continue; }

                }
                else*/ if (repo.GetType() == typeof(string))
                {
                    WebClient wc = new WebClient();
                    wc.BaseAddress = repo.ToString();
                    try
                    {
                        //if (wc.DownloadString(".status").Contains("online"))
                        {
                            if (wc.DownloadString($"{BaseAddress}/.status").Contains("online"))
                            {
                                _http = repo.ToString();
                                _repository = wc;
                                break;
                            }
                        }
                    }
                    catch { continue; }

                }
            }
            if (this._repository == null)
            {
                throw new NullReferenceException();
            }
        }
        /// <summary>
        /// Download a file from remote repository into byte array
        /// </summary>
        /// <param name="relative">Relative path to the file</param>
        /// <returns></returns>
        public byte[] GetFile(string relative)
        {
            //string addr = (BaseAddress.Length > 0) ? $"{BaseAddress}/" : "";
            //relative = addr + relative;
            Console.WriteLine(relative);
            if (relative[0] == '/' && BaseAddress.Length > 0) relative = relative.Substring(1);

            //if (!IsDropbox)
                using (WebClient wc = new WebClient() { BaseAddress = _http + this.BaseAddress })
                {
                    wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    return wc.DownloadData(relative.Replace("//", "/"));
                }
            //else
             //   return (_repository as DropNetClient).GetFile(relative);
        }

        /// <summary>
        /// Download a file from remote repository and save it to disk
        /// </summary>
        /// <param name="relative" >Relative path to the file</param>
        public void DownloadFile(string relative, string path)
        {
            byte[] buffer = this.GetFile(relative);
            using (FileStream fs = File.OpenWrite(path))
            {
                fs.Write(buffer, 0, buffer.Length);
            }
            buffer = null;
        }
        /// <summary>
        /// Download a string file from remote repository
        /// </summary>
        /// <param name="relative">Relative path to the file</param>
        /// <returns></returns>
        public string GetString(string relative)
        {
            return Encoding.UTF8.GetString(GetFile(relative));
        }

    }
}
