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
            return constellations[shortname];
        }

        /// <summary>
        /// Loads constellation boundary data from the file.
        /// </summary>
        /// <param name="filepath">Path to contellation data file</param>
        /// <returns>
        /// A dictionary where the key is the constellation abbreviation (e.g., "UMA") and the value is a list of (RA, Dec) coordinate pairs forming its boundary.
        /// </returns>
        public static Dictionary<string, List<(double, double)>> LoadConstellationMap(string filepath = "bound_20.dat")
        {
            Dictionary<string, List<(double, double)>> borders = new Dictionary<string, List<(double, double)>>();

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

                for (int i = 0; i < n; i++)
                {
                    var l = coords[(i - 1 + n) % n]; // wrap around
                    var r = coords[i];

                    double l_ra = l.Item1, l_dec = l.Item2;
                    double r_ra = r.Item1, r_dec = r.Item2;

                    // Swap if needed
                    if (r_ra < l_ra)
                    {
                        (l_ra, r_ra) = (r_ra, l_ra);
                        (l_dec, r_dec) = (r_dec, l_dec);
                    }

                    bool isCrossing = (r_ra - l_ra) > 5;

                    if (!isCrossing)
                    {
                        if (ra < l_ra || ra >= r_ra)
                            continue;
                    }
                    else
                    {
                        if (l_ra <= ra && ra < r_ra)
                            continue;
                    }

                    if (dec <= l_dec && dec <= r_dec)
                    {
                        intersects++;
                    }
                    else if ((l_dec < dec && dec <= r_dec) || (r_dec < dec && dec <= l_dec))
                    {
                        onBoundary = true;
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
    }
}
