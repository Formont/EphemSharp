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
            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);    
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    string[] data = line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    string name = data[0];
                    List<(int, int)> stars = new List<(int, int)>();
                    for (int i = 2; i < data.Length; i+=2)
                    {
                        stars.Add((int.Parse(data[i]), int.Parse(data[i + 1])));
                    }
                    (string, List<(int, int)>) constellation = (name, stars);
                    constellations.Add(constellation);
                }
            }
            return constellations;
        }
    }
}
