using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemSharp
{
    public static class Loader
    {
        public static void Load(string url, string filename)
        {
            if (!File.Exists(filename))
            {
                using (WebClient client = new WebClient())
                {
                    Console.WriteLine("Downloading...");
                    client.DownloadFile(url, filename);
                }
            }
        }
    }
}
