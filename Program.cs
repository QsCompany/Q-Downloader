using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
// ReSharper disable ConvertToAutoProperty
using System;
using System.IO;

namespace QsDownloader
{
    class Program
    {
        static void Main(string[] args)
        {

           //DDD();
            args = new[]
            {
                "http://www.eset.com/us/resources/white-papers/Stuxnet_Under_the_Microscope.pdf",
                // "http://127.0.0.2:80/",
                "e:\\test\\",
                "3"
            };
            var url = new Uri(args[0]);
            var outp = args[1];
            var cns = (int) double.Parse(args[2]);
            var mt = MultiTask.Create(url, outp, null, cns*2);
            mt.Begin();
            mt.Dispose();
            var OpenFolderContainer_OpenFile = 0 != Math.Abs(0);
            var eSelect = OpenFolderContainer_OpenFile ? "-p /select," : "/e /select,";
            p.Arguments = eSelect + mt.FullName;
            var ps = new Process { StartInfo = p };
            if (mt.TotalyDownloaded)
                ps.Start();
        }
        static ProcessStartInfo p = new ProcessStartInfo
        {
            FileName = "explorer",
            
            UseShellExecute = true,
            CreateNoWindow = false
        };
    }

   static class Serialler<T> where T:class
   {
       public static void WriteObject(string fileName,T p1)
       {
           var writer = new FileStream(fileName, FileMode.Create);
           var ser = new DataContractSerializer(typeof (T));
           ser.WriteObject(writer, p1);
           writer.Close();
       }
       
       public static T ReadObject(string fileName)
       {
           try
           {
               var fs = new FileStream(fileName, FileMode.Open);
               var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
               var ser = new DataContractSerializer(typeof (T));
               var deserializedPerson = (T) ser.ReadObject(reader, true);
               reader.Close();
               fs.Close();
               return deserializedPerson;
           }
           catch
           {
               return null;
           }
       }
   }
}
