using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EphemSharp.Bodies;
using EphemSharp.Enums;
using EphemSharp.Units;

namespace EphemSharp.Data
{
    public static class Catalogs
    {
        public const string HIP = "https://cdsarc.cds.unistra.fr/ftp/cats/I/239/hip_main.dat";
        public static Dictionary<string, Star> LoadHipparcos(string filename = "hip_main.dat")
        {
            Loader.Load(HIP, filename);

            var stars = new Dictionary<string, Star>();
            string[] lines = File.ReadAllLines(filename);

            foreach (var line in lines)
            {
                string[] parts = line.Split('|');
                var ra_data = parts[3].Split(' ');
                Angle ra = new Angle(AngleType.Hours, int.Parse(ra_data[0]),
                    int.Parse(ra_data[1]), double.Parse(ra_data[2], CultureInfo.InvariantCulture));
                var dec_data = parts[4].Split(' ');
                Angle dec = new Angle(AngleType.Degrees, int.Parse(dec_data[0]),
                    int.Parse(dec_data[1]), double.Parse(dec_data[2], CultureInfo.InvariantCulture));
                Star star = new Star(ra, dec);
                star.Magnitude = ParseDouble(parts[5]);
                star.ParallaxMas = ParseDouble(parts[11]);
                star.RaMasPerYear = ParseDouble(parts[12]);
                star.DecMasPerYear = ParseDouble(parts[13]);
                stars.Add(parts[1].Trim(),star);
            }

            return stars;
        }

        private static double ParseDouble(string s)
        {
            try
            {
                return double.Parse(s.Trim(), CultureInfo.InvariantCulture);
            }
            catch
            {
                double val;
                if (double.TryParse(s.Trim(), out val))
                {
                    return val;
                }
                return 0;
            }
        }
    }
}
