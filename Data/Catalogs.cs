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
                if (parts.Length < 14) continue;

                var ra_data = parts[3].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int ra_h = ra_data.Length > 0 && int.TryParse(ra_data[0], out int rh) ? rh : 0;
                int ra_m = ra_data.Length > 1 && int.TryParse(ra_data[1], out int rm) ? rm : 0;
                double ra_s = ra_data.Length > 2 && double.TryParse(ra_data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double rs) ? rs : 0.0;
                Angle ra = new Angle(AngleType.Hours, ra_h, ra_m, ra_s);

                var dec_data = parts[4].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int dec_d = dec_data.Length > 0 && int.TryParse(dec_data[0], out int dd) ? dd : 0;
                int dec_m = dec_data.Length > 1 && int.TryParse(dec_data[1], out int dm) ? dm : 0;
                double dec_s = dec_data.Length > 2 && double.TryParse(dec_data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double ds) ? ds : 0.0;
                Angle dec = new Angle(AngleType.Degrees, dec_d, dec_m, dec_s);

                Star star = new Star(ra, dec);
                star.Magnitude = ParseDouble(parts[5]);
                star.ParallaxMas = ParseDouble(parts[11]);
                star.RaMasPerYear = ParseDouble(parts[12]);
                star.DecMasPerYear = ParseDouble(parts[13]);
                stars.Add(parts[1].Trim(), star);
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
