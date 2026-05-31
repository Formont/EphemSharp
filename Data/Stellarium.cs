using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace EphemSharp.Data
{
    public static class Stellarium
    {
        public static List<(string, List<(int, int)>)> LoadConstellations(string filename)
        {
            List<(string, List<(int, int)>)> constellations = new List<(string, List<(int, int)>)>();
            if (!File.Exists(filename)) return constellations;

            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);    
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    string[] data = trimmedLine.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 3) continue;

                    string name = data[0];
                    List<(int, int)> stars = new List<(int, int)>();
                    for (int i = 2; i + 1 < data.Length; i += 2)
                    {
                        if (int.TryParse(data[i], out int star1) && int.TryParse(data[i + 1], out int star2))
                        {
                            stars.Add((star1, star2));
                        }
                    }
                    constellations.Add((name, stars));
                }
            }
            return constellations;
        }
    }
}
