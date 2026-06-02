using EphemSharp.Units;
using System;
using EphemSharp.Bodies;

namespace EphemSharp.Data
{
    public static class Moon
    {
        public static (Vector xyz, Distance r) XYZR(double jd)
        {
            double d = jd - 2451545.0; // Days since J2000 epoch

            double rad = Math.PI / 180.0;

            // Geocentric ecliptic coordinates of the Moon (from SunCalc / Astronomy Answers)
            double L = rad * (218.316 + 13.176396 * d); // Ecliptic longitude
            double M = rad * (134.963 + 13.064993 * d); // Mean anomaly
            double F = rad * (93.272 + 13.229350 * d);  // Mean distance

            double l = L + rad * 6.289 * Math.Sin(M);        // Longitude
            double b = rad * 5.128 * Math.Sin(F);            // Latitude
            double dt_km = 385001 - 20905 * Math.Cos(M);     // Distance in km
            double dt_au = dt_km / Distance.AU_KM;           // Distance in AU

            // Geocentric ecliptic coordinates
            double x_geo = dt_au * Math.Cos(b) * Math.Cos(l);
            double y_geo = dt_au * Math.Cos(b) * Math.Sin(l);
            double z_geo = dt_au * Math.Sin(b);

            // Get Earth's heliocentric position
            var earthPos = Earth.XYZR(jd);

            // Moon's heliocentric position = Earth's heliocentric position + Moon's geocentric position
            Vector moonHeliocentricXyz = earthPos.xyz + new Vector(x_geo, y_geo, z_geo);

            return (moonHeliocentricXyz, new Distance(au: moonHeliocentricXyz.Length()));
        }
    }
}
