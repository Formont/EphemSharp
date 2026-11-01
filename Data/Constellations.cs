using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

namespace EphemSharp.Data
{
    public static class Constellations
    {
        static string GetFullName(string shortname)
        {
            if (string.IsNullOrEmpty(shortname))
            {
                return "Unknown";
            }

            Dictionary<string, string> constellations = new Dictionary<string, string>
            {
                { "AQL", "Aquila" },
                { "AND", "Andromeda" },
                { "SCL", "Sculptor" },
                { "ARA", "Ara" },
                { "LIB", "Libra" },
                { "CET", "Cetus" },
                { "ARI", "Aries" },
                { "PYX", "Pyxis" },
                { "BOO", "Bootes" },
                { "CAE", "Caelum" },
                { "CHA", "Chamaeleon" },
                { "CNC", "Cancer" },
                { "CAP", "Capricornus" },
                { "CAR", "Carina" },
                { "CAS", "Cassiopeia" },
                { "CEN", "Centaurus" },
                { "CEP", "Cepheus" },
                { "COM", "Coma Berenices" },
                { "CVN", "Canes Venatici" },
                { "AUR", "Auriga" },
                { "COL", "Columba" },
                { "CIR", "Circinus" },
                { "CRT", "Crater" },
                { "CRA", "Corona Australis" },
                { "CRB", "Corona Borealis" },
                { "CRV", "Corvus" },
                { "CRU", "Crux" },
                { "CYG", "Cygnus" },
                { "DEL", "Delphinus" },
                { "DOR", "Dorado" },
                { "DRA", "Draco" },
                { "NOR", "Norma" },
                { "ERI", "Eridanus" },
                { "SGE", "Sagitta" },
                { "FOR", "Fornax" },
                { "GEM", "Gemini" },
                { "CAM", "Camelopardalis" },
                { "CMA", "Canis Major" },
                { "UMA", "Ursa Major" },
                { "GRU", "Grus" },
                { "HER", "Hercules" },
                { "HOR", "Horologium" },
                { "HYA", "Hydra" },
                { "HYI", "Hydrus" },
                { "IND", "Indus" },
                { "LAC", "Lacerta" },
                { "MON", "Monoceros" },
                { "LEP", "Lepus" },
                { "LEO", "Leo" },
                { "LUP", "Lupus" },
                { "LYN", "Lynx" },
                { "LYR", "Lyra" },
                { "ANT", "Antlia" },
                { "MIC", "Microscopium" },
                { "MUS", "Musca" },
                { "OCT", "Octans" },
                { "APS", "Apus" },
                { "OPH", "Ophiuchus" },
                { "ORI", "Orion" },
                { "PAV", "Pavo" },
                { "PEG", "Pegasus" },
                { "PIC", "Pictor" },
                { "PER", "Perseus" },
                { "EQU", "Equuleus" },
                { "CMI", "Canis Minor" },
                { "LMI", "Leo Minor" },
                { "VUL", "Vulpecula" },
                { "UMI", "Ursa Minor" },
                { "PHE", "Phoenix" },
                { "PSC", "Pisces" },
                { "PSA", "Piscis Austrinus" },
                { "VOL", "Volans" },
                { "PUP", "Puppis" },
                { "RET", "Reticulum" },
                { "SGR", "Sagittarius" },
                { "SCO", "Scorpius" },
                { "SCT", "Scutum" },
                { "SER", "Serpens" },
                { "SEX", "Sextans" },
                { "MEN", "Mensa" },
                { "TAU", "Taurus" },
                { "TEL", "Telescopium" },
                { "TUC", "Tucana" },
                { "TRI", "Triangulum" },
                { "TRA", "Triangulum Australe" },
                { "AQR", "Aquarius" },
                { "VIR", "Virgo" },
                { "VEL", "Vela" }
            };

            if (shortname.Contains("+"))
            {
                string[] parts = shortname.Split('+');
                List<string> fullNames = new List<string>();
                foreach (var p in parts)
                {
                    string trimmed = p.Trim();
                    if (constellations.TryGetValue(trimmed, out string fullName))
                    {
                        fullNames.Add(fullName);
                    }
                    else
                    {
                        fullNames.Add(trimmed);
                    }
                }
                return string.Join(" + ", fullNames);
            }

            if (constellations.TryGetValue(shortname, out string name))
            {
                return name;
            }
            return shortname;
        }

        /// <summary>
        /// Loads constellation boundary data from the file.
        /// </summary>
        /// <param name="filepath">Path to contellation data file</param>
        /// <returns>
        /// A dictionary where the key is the constellation abbreviation (e.g., "UMA") and the value is a list of (RA, Dec) coordinate pairs forming its boundary.
        /// </returns>
        public static Dictionary<string, List<(double, double)>> LoadConstellationMap(string filepath = null)
        {
            Dictionary<string, List<(double, double)>> borders = new Dictionary<string, List<(double, double)>>();

            if (string.IsNullOrWhiteSpace(filepath))
            {
                string outputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bound_20.dat");
                string projectFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "bound_20.dat");
                string workingDirectoryFile = Path.Combine(Directory.GetCurrentDirectory(), "files", "bound_20.dat");
                filepath = File.Exists(outputFile) ? outputFile :
                    (File.Exists(projectFile) ? projectFile : workingDirectoryFile);
            }

            using (StreamReader sr = new StreamReader(filepath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] parts = line.Split();
                    double ra = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    double dec = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    string constName = parts[2];

                    if (!borders.ContainsKey(constName))
                    {
                        borders[constName] = new List<(double, double)>();
                    }
                    borders[constName].Add((ra, dec));
                }
            }

            return borders;
        }

        /// <summary>
        /// Determines which constellation a celestial object belongs to, based on its equatorial coordinates (RA and Dec).
        /// </summary>
        /// <param name="body">Celestial body.</param>
        /// <param name="constellations">A dictionary containing constellation boundaries, as returned by <see cref="LoadConstellationMap"/>.</param>
        /// <returns>
        /// The name of the constellation. If the point lies on the boundary of two constellations, both names are returned separated by '+'.  
        /// If the coordinates are near the north celestial pole (Dec > 85) and not within any other boundary, returns "UMI".
        /// </returns>
        public static string FindConstellation(CelestialBody body, Dictionary<string, List<(double, double)>> constellations)
        {
            if (body == null) throw new ArgumentNullException("body");
            if (constellations == null) throw new ArgumentNullException("constellations");
            double ra = body.RightAscension.GetHours();
            double dec = body.Declination.GetDegrees();
            string result = "";

            foreach (var entry in constellations)
            {
                string constName = entry.Key;
                var coords = entry.Value;
                int intersects = 0;
                bool onBoundary = false;
                int n = coords.Count;
                if (n < 3) continue;

                for (int i = 0; i < n; i++)
                {
                    var left = coords[(i - 1 + n) % n];
                    var right = coords[i];
                    double x1 = NormalizeHours(left.Item1 - ra);
                    double x2 = x1 + NormalizeHours(right.Item1 - left.Item1);
                    // Move this edge to the copy of the 24-hour RA circle nearest RA=0.
                    double shift = 24.0 * Math.Round((x1 + x2) / 48.0);
                    x1 -= shift;
                    x2 -= shift;
                    double y1 = left.Item2;
                    double y2 = right.Item2;

                    if (Math.Abs(x1) < 1e-12 && Math.Abs(x2) < 1e-12)
                    {
                        if (dec >= Math.Min(y1, y2) && dec <= Math.Max(y1, y2)) onBoundary = true;
                        continue;
                    }

                    if ((x1 <= 0 && x2 >= 0) || (x2 <= 0 && x1 >= 0))
                    {
                        double yAtRa = y1 + (y2 - y1) * (-x1) / (x2 - x1);
                        if (Math.Abs(yAtRa - dec) < 1e-9) onBoundary = true;
                    }

                    // Cast a ray toward increasing declination from the query point.
                    if ((x1 > 0) != (x2 > 0))
                    {
                        double yAtRa = y1 + (y2 - y1) * (-x1) / (x2 - x1);
                        if (yAtRa > dec) intersects++;
                    }
                }

                if (onBoundary)
                {
                    if (result == "")
                        result = constName;
                    else
                        result += "+" + constName;
                    continue;
                }

                if (intersects % 2 == 1)
                {
                    result = constName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(result) && dec > 85)
            {
                result = "UMI";
            }

            return GetFullName(result);
        }

        private static double NormalizeHours(double hours)
        {
            hours %= 24.0;
            if (hours <= -12.0) hours += 24.0;
            else if (hours > 12.0) hours -= 24.0;
            return hours;
        }
    }
}
