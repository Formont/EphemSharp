using System;
using System.IO;
using System.Net.Http;

namespace EphemSharp
{
    public static class Loader
    {
        public static void Load(string url, string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("Downloading...");
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(url).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(filename, FileMode.CreateNew))
                    {
                        response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                    }
                }
            }
        }
    }
}
