using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

namespace QsDownloader
{
    [Serializable]
    [DataContract(Name = "Task", Namespace = "WWW.Task.Com",IsReference=true)]
    public class Task:IDisposable
    {
        internal MultiTask Parent;
        const int BUFFER_SIZE = 16 * 1024;
        [DataMember]
        public long From { get; private set; }
        [DataMember]
        public long To { get; private set; }
        [DataMember]
        public long BytesReaded { get; private set; }
        [DataMember]
        public readonly string DestinationPATH;
        [DataMember]
        public readonly Uri SourceURL;
        [DataMember]
        private bool Resume;

        internal bool initialized;

        private FileStream outputFileStream;
        private HttpWebRequest req;
        private WebResponse response;
        private Stream responseStream;

        public Thread Thread;
        public Task(Uri url, string destinationPath,long from,long to)
        {
            SourceURL = url;
            DestinationPATH = destinationPath;
            From = from;
            To = to;
        }

        internal static string FileName(Uri url)
        {
            var g = url.ToString();
            for (var i = g.Length - 1; i >= 0; i--)
                if (g[i] == '\\' || g[i] == '/')
                    return i == g.Length - 1 ? "" : g.Substring(i + 1);
            return g;
        }
        private bool OpenFile(bool forWrite)
        {
            if (outputFileStream != null) outputFileStream.Close();
            bool l;
            if (!(l=File.Exists(DestinationPATH)) && !forWrite) return false;
            outputFileStream = new FileStream(DestinationPATH, FileMode.OpenOrCreate,
                forWrite ? FileAccess.Write : FileAccess.Read,
                FileShare.Inheritable, BUFFER_SIZE, FileOptions.None);
            return l;
        }

        public bool Initialize()
        {
            try
            {
               Resume= OpenFile(true);
                if (Resume)
                {
                    if (outputFileStream.Length < BytesReaded)
                        BytesReaded = outputFileStream.Length;
                    outputFileStream.Seek(BytesReaded, SeekOrigin.Begin);
                }
                req = (HttpWebRequest) WebRequest.Create(SourceURL);
                req.AddRange(From + BytesReaded, To);
                req.AllowAutoRedirect = true;
                req.KeepAlive = true;
                req.Pipelined = true;

                return initialized = true;
            }
            catch (Exception)
            {
                return initialized = false;
            }
        }

        internal bool IsComplete;

        public bool Begin(MultiTask multiTask)
        {
            if (!initialized) return Percent >= 100;
            initialized = false;
            var buffer = new byte[BUFFER_SIZE];
            try
            {
                using (response = req.GetResponse())
                {
                    using (responseStream = response.GetResponseStream())
                    {
                        int bytesRead;
                        if (responseStream.CanSeek)
                            responseStream.Seek(100, SeekOrigin.Begin);
                        do
                        {
                            bytesRead = responseStream.Read(buffer, 0, BUFFER_SIZE);
                            outputFileStream.Write(buffer, 0, bytesRead);
                            BytesReaded += bytesRead;
                        } while (bytesRead > 0 && !multiTask.Exit);
                        IsComplete = (To - From + 1) == BytesReaded;
                    }
                }
            }
            catch(Exception)
            {
                IsComplete = false;
            }
            Dispose();
            return IsComplete;
        }

        public float Percent
        {
            get { return BytesReaded/((float) (To - From))*100.0f; }
        }

        public void Dispose()
        {
            initialized = false;
            if (responseStream != null) responseStream.Close();
            if (response != null) response.Close();
            if (outputFileStream != null)
                outputFileStream.Close();
            if (outputFileStream != null)
                outputFileStream.Dispose();
        }

        public bool CopyTo(FileStream re)
        {
            var buffer = new byte[BUFFER_SIZE];
            int bs;
            if (OpenFile(false))
                do
                    re.Write(buffer, 0, bs = outputFileStream.Read(buffer, 0, BUFFER_SIZE)); while (bs > 0);
            else return false;
            outputFileStream.Close();
            File.Delete(DestinationPATH);
            return true;
        }
    }
}