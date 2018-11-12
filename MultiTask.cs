using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;

namespace QsDownloader
{
    [Serializable]
    [DataContract(Name = "MultiTask", Namespace = "WWW.MultiTask.Com", IsReference = true)]
    public class MultiTask:IDisposable
    {
        private List<Task> _tasks1;
        public IReadOnlyList<Task> Tasks { get; private set; }

        [DataMember] 
        private List<Task> tasks
        {
            get { return _tasks1; }
            set
            {
                _tasks1 = value;
                Tasks = value.AsReadOnly();
            }
        }

        [DataMember]
        public string DestinationPATH { get; private set; }

        [DataMember]
        public Uri SourceURL { get; private set; }

        [DataMember]
        public int Connections { get; private set; }

        [DataMember]
        public long Size { get; private set; }

        [DataMember]
        public long DSize { get; private set; }

        [DataMember]
        public long Step { get; private set; }


        [DataMember]
        public WebHeaderCollection RequestHeader { get; private set; }

        [DataMember]
        private bool initialized;

        [DataMember] public string FileName;

        public string FullName
        {
            get { return Path.Combine(DestinationPATH, FileName); }
        }

        public static MultiTask Create(Uri sourceURI, string destinationPATH,string fileName=null, int connection = 0x2)
        {
            var FileName = fileName ?? Task.FileName(sourceURI);
            var _ret = Serialler<MultiTask>.ReadObject(Path.Combine(destinationPATH, FileName) + ".xml");
            return _ret ?? new MultiTask(sourceURI, destinationPATH,FileName, connection);
        }

        private MultiTask(Uri sourceURI, string destinationPATH, string fileName, int connection)
        {
            tasks = new List<Task>();
            FileName = fileName;
            SourceURL = sourceURI;
            DestinationPATH = destinationPATH;
            TempDirectory = Guid.NewGuid().ToString();
            Connections = connection;
            Tasks = tasks.AsReadOnly();
            Initialize();
        }

        public static long GetSize(Uri url,out WebHeaderCollection webHeader)
        {
            var req = WebRequest.Create(url);
            req.Method = "HEAD";
            req.Timeout = 4000;
            using ( var resp = req.GetResponse())
            {
                webHeader = req.Headers;
                int ContentLength;
                return int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength) && ContentLength != -1
                    ? ContentLength
                    : req.ContentLength;
            }
        }
        [DataMember]
        private string TempDirectory;
        private void Initialize()
        {
            WebHeaderCollection header;
            Size = GetSize(SourceURL, out header);
            if (header == null || Size == 0)            
                return;            
            RequestHeader = header;
            Step = Size/Connections;
            long np = 0, pp = 0;
            var path = Path.Combine(DestinationPATH,  TempDirectory + "\\");
            Directory.CreateDirectory(path);
            for (var i = 0; i < Connections; i++)
            {
                if (i == Connections-1)
                    np = Size - 1;
                else np += Step;
                
                var task = new Task(SourceURL, Path.Combine(path, Guid.NewGuid().ToString()), pp, np);
                task.Initialize();
                tasks.Add(task);
                pp = np + 1;
            }
        }

        public bool Begin()
        {
            foreach (var task in tasks)
                BeginTask(task);
            foreach (var task in tasks)
            {
            db:
                Console.Clear();
                foreach (var task1 in tasks)
                    Console.WriteLine("task 1: IsComplete({0}) ::{1}  :::::{2}", task1.IsComplete, task1.Percent, task1.Thread.IsAlive);
                if (task.IsComplete || !task.Thread.IsAlive)
                    continue;
                Thread.Sleep(500);
                Serialler<MultiTask>.WriteObject(FullName + ".xml", this);
                goto db;
            }
            var complete = true;
            var re = new FileStream(FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Inheritable,
                (int)Size, FileOptions.WriteThrough);
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.Percent < 100)
                {
                    complete = false;
                    break;
                }
                if (!task.CopyTo(re)) continue;
                tasks.RemoveAt(i);
                i--;
            }
            re.Close();
            if (!complete) return false;
            TotalyDownloaded = true;
            File.Delete(FullName+".xml");
            return true;
        }

        public bool TotalyDownloaded = false;
        public bool Exit;
        private void BeginTask(Task task)
        {
            task.Thread = new Thread(new ParameterizedThreadStart(delegate
            {
                db:
                if (Exit || task.IsComplete || task.Begin(this))
                {
                }
                else
                {
                    if (!task.initialized) task.Initialize();
                    goto db;
                }
            }));
            task.Thread.Start();
        }

        public void Dispose()
        {
            foreach (var task in Tasks)
                task.Dispose();
            if (!TotalyDownloaded)
                Serialler<MultiTask>.WriteObject(FullName + ".xml", this);
        }
    }
}