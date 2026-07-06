using System;

namespace EphemSharp
{
    /// <summary>
    /// Represents an astronomical time, supporting two-part Julian dates and multiple time scales (UTC, UT1, TT, TDB).
    /// </summary>
    public class AstroTime
    {
        /// <summary>
        /// Integer part of the Julian Date.
        /// </summary>
        public double JdInteger { get; private set; }

        /// <summary>
        /// Fractional part of the Julian Date (0.0 to 1.0).
        /// </summary>
        public double JdFraction { get; private set; }

        /// <summary>
        /// The combined Universal Time Julian Date (UTC).
        /// </summary>
        public double JD => JdInteger + JdFraction;

        /// <summary>
        /// Difference between Terrestrial Time (TT) and Universal Time (UT) in seconds.
        /// </summary>
        public double DeltaT { get; private set; }

        /// <summary>
        /// Initializes a new AstroTime from a standard UTC DateTime.
        /// </summary>
        public AstroTime(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local)
                utc = utc.ToUniversalTime();

            double jd = ToJulianDate(utc);
            JdInteger = Math.Floor(jd);
            JdFraction = jd - JdInteger;

            double decimalYear = utc.Year + (utc.Month - 0.5) / 12.0;
            DeltaT = CalculateDeltaT(decimalYear);
        }

        /// <summary>
        /// Initializes a new AstroTime from a UTC Julian Date.
        /// </summary>
        public AstroTime(double jd)
        {
            JdInteger = Math.Floor(jd);
            JdFraction = jd - JdInteger;

            // Approximate decimal year
            double jc = (jd - 2451545.0) / 36525.0;
            double decimalYear = 2000.0 + jc * 100.0;
            DeltaT = CalculateDeltaT(decimalYear);
        }

        // Time Scales (Julian Dates)
        public double UTC_JD => JD;
        
        // UT1 ~ UTC for now, but can be adjusted later if DeltaUT1 is needed
        public double UT1_JD => UTC_JD;

        // TT = UTC + DeltaT
        public double TT_JD => UTC_JD + (DeltaT / 86400.0);

        // TDB ~ TT in most practical applications (difference is < 2ms)
        public double TDB_JD => TT_JD;

        // Julian Centuries
        public double JulianCenturiesTT => (TT_JD - 2451545.0) / 36525.0;
        public double JulianCenturiesTDB => (TDB_JD - 2451545.0) / 36525.0;

        /// <summary>
        /// Calculates Delta T (TT - UT) in seconds for a given decimal year 
        /// using Espenak and Meeus polynomial expressions.
        /// </summary>
        /// 
        public static double CalculateDeltaT(double y)
        {
            double t, u;
            if (y < -500)
            {
                u = (y - 1820) / 100.0;
                return -20 + 32 * u * u;
            }
            else if (y < 500)
            {
                u = y / 100.0;
                return 10583.6 - 1014.41 * u + 33.78311 * u * u - 5.952053 * u * u * u 
                       - 0.1798452 * Math.Pow(u, 4) + 0.022174192 * Math.Pow(u, 5) + 0.0090316521 * Math.Pow(u, 6);
            }
            else if (y < 1600)
            {
                u = (y - 1000) / 100.0;
                return 1574.2 - 556.01 * u + 71.23472 * u * u + 0.319781 * u * u * u 
                       - 0.8503463 * Math.Pow(u, 4) - 0.005050998 * Math.Pow(u, 5) + 0.0083572073 * Math.Pow(u, 6);
            }
            else if (y < 1700)
            {
                t = y - 1600;
                return 120 - 0.9808 * t - 0.01532 * t * t + Math.Pow(t, 3) / 7129.0;
            }
            else if (y < 1800)
            {
                t = y - 1700;
                return 8.83 + 0.1603 * t - 0.0059285 * t * t + 0.00013336 * t * t * t - Math.Pow(t, 4) / 1174000.0;
            }
            else if (y < 1860)
            {
                t = y - 1800;
                return 13.72 - 0.332447 * t + 0.0068612 * t * t + 0.0041116 * t * t * t - 0.00037436 * Math.Pow(t, 4) 
                       + 0.0000121272 * Math.Pow(t, 5) - 0.0000001699 * Math.Pow(t, 6) + 0.000000000875 * Math.Pow(t, 7);
            }
            else if (y < 1900)
            {
                t = y - 1860;
                return 7.62 + 0.5737 * t - 0.251754 * t * t + 0.01680668 * t * t * t - 0.0004473624 * Math.Pow(t, 4) + Math.Pow(t, 5) / 233174.0;
            }
            else if (y < 1920)
            {
                t = y - 1900;
                return -2.79 + 1.494119 * t - 0.0598939 * t * t + 0.0061966 * t * t * t - 0.000197 * Math.Pow(t, 4);
            }
            else if (y < 1941)
            {
                t = y - 1920;
                return 21.20 + 0.84493 * t - 0.076100 * t * t + 0.0020936 * t * t * t;
            }
            else if (y < 1961)
            {
                t = y - 1950;
                return 29.07 + 0.407 * t - Math.Pow(t, 2) / 233.0 + Math.Pow(t, 3) / 2547.0;
            }
            else if (y < 1986)
            {
                t = y - 1975;
                return 45.45 + 1.067 * t - Math.Pow(t, 2) / 260.0 - Math.Pow(t, 3) / 718.0;
            }
            else if (y < 2005)
            {
                t = y - 2000;
                return 63.86 + 0.3345 * t - 0.060374 * t * t + 0.0017275 * t * t * t + 0.000651814 * Math.Pow(t, 4) + 0.00002373599 * Math.Pow(t, 5);
            }
            else if (y < 2050)
            {
                t = y - 2000;
                return 62.92 + 0.32217 * t + 0.005589 * t * t;
            }
            else if (y < 2150)
            {
                t = (y - 1820) / 100.0;
                return -20 + 32 * t * t - 0.5628 * (2150 - y);
            }
            else
            {
                t = (y - 1820) / 100.0;
                return -20 + 32 * t * t;
            }
        }

        public static double ToJulianDate(DateTime dateTime)
        {
            int year = dateTime.Year;
            int month = dateTime.Month;
            double day = dateTime.Day +
                         dateTime.Hour / 24.0 +
                         dateTime.Minute / 1440.0 +
                         dateTime.Second / 86400.0 +
                         dateTime.Millisecond / 86400000.0;

            if (month <= 2)
            {
                year--;
                month += 12;
            }

            int A = year / 100;
            int B = 2 - A + (A / 4);

            double jd = Math.Floor(365.25 * (year + 4716))
                      + Math.Floor(30.6001 * (month + 1))
                      + day + B - 1524.5;

            return jd;
        }
    }
}
