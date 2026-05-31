using System;
using EphemSharp.Utils;
using EphemSharp.Units;
using EphemSharp.Enums;

namespace EphemSharp
{
    /// <summary>
    /// Represents a geographic observer on Earth, used to compute the visible horizontal coordinates of celestial bodies.
    /// </summary>
    public class Observer
    {
        /// <summary>
        /// Geographic latitude of the observer in degrees (positive is North, negative is South).
        /// </summary>
        double Latitude { get; set; }

        /// <summary>
        /// Geographic longitude of the observer in degrees (positive is East, negative is West).
        /// </summary>
        double Longitude { get; set; }

        /// <summary>
        /// Elevation of the observer above sea level in meters.
        /// </summary>
        double Elevation { get; set; }  

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class at 0 latitude and 0 longitude.
        /// </summary>
        public Observer() 
        { 
            Latitude = 0;
            Longitude = 0;
            Elevation = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class with specific geographic coordinates.
        /// </summary>
        /// <param name="latitude">The geographic latitude in degrees.</param>
        /// <param name="longitude">The geographic longitude in degrees.</param>
        /// <param name="elevation">The elevation above sea level in meters (defaults to 0).</param>
        public Observer(double latitude, double longitude, double elevation = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }

        /// <summary>
        /// Observes a celestial body at the current UTC time.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body)
        {
            return Observe(body, Time.ToJulianDate(DateTime.UtcNow));
        }

        /// <summary>
        /// Observes a celestial body at a specific UTC date and time.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <param name="utc">The date and time in UTC.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body, DateTime utc)
        {
            return Observe(body, Time.ToJulianDate(utc));
        }

        /// <summary>
        /// Observes a celestial body at a specific Julian Date.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <param name="jd">The Julian Date.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body, double jd)
        {
            double[] azah = RaDecToAltAz(body.RightAscension.Radians,
                body.Declination.Radians,
                ((Angle)this.Latitude).Radians,
                ((Angle)this.Longitude).Radians,
                jd
            );

            Angle alt = new Angle(AngleType.Degrees, azah[0]);
            Angle az = new Angle(AngleType.Degrees, azah[1]);
            Angle h = new Angle(AngleType.Hours, azah[2]);
            return new ObservedObject(alt, az, h);
        }

        /// <summary>
        /// Converts equatorial coordinates (Right Ascension and Declination) to horizontal coordinates (Altitude and Azimuth).
        /// </summary>
        /// <param name="ra">Right Ascension in radians.</param>
        /// <param name="dec">Declination in radians.</param>
        /// <param name="lat">Observer's latitude in radians.</param>
        /// <param name="lon">Observer's longitude in radians.</param>
        /// <param name="jd">Julian Date.</param>
        /// <returns>An array containing Altitude [0], Azimuth [1], and Hour Angle [2].</returns>
        static double[] RaDecToAltAz(double ra, double dec, double lat, double lon, double jd)
        {
            // Meeus 13.5 and 13.6, modified so West longitudes are negative and 0 is North
            double jd_ut = jd;
            double gmst = GreenwichMeanSiderealTime(jd_ut);
            double localSiderealTime = (gmst + lon) % (2 * Math.PI);

            double H = (localSiderealTime - ra);
            if (H < 0) { H += 2 * Math.PI; }
            if (H > Math.PI) { H -= 2 * Math.PI; }

            double az = Math.Atan2(Math.Sin(H), Math.Cos(H) * Math.Sin(lat) - Math.Tan(dec) * Math.Cos(lat));
            double a = Math.Asin(Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(H));
            az -= Math.PI;

            if (az < 0) { az += 2 * Math.PI; }
            return new double[] { a, az, H };
        }

        /// <summary>
        /// Calculates Greenwich Mean Sidereal Time (GMST) for a given Julian Date using the Capitaine et al. (IAU 2000) expressions.
        /// </summary>
        /// <param name="jd">Julian Date.</param>
        /// <returns>Greenwich Mean Sidereal Time in radians.</returns>
        public static double GreenwichMeanSiderealTime(double jd)
        {
            // "Expressions for IAU 2000 precession quantities" N. Capitaine1,P.T.Wallace2, and J. Chapront
            double t = (jd - 2451545.0) / 36525.0;

            double gmst = EarthRotationAngle(jd) + (0.014506 + 4612.156534 * t + 1.3915817 * t * t - 0.00000044 * t * t * t - 0.000029956 * t * t * t * t - 0.0000000368 * t * t * t * t * t) / 60.0 / 60.0 * Math.PI / 180.0;  // eq 42
            gmst %= 2 * Math.PI;
            if (gmst < 0) gmst += 2 * Math.PI;

            return gmst;
        }

        /// <summary>
        /// Calculates the Earth Rotation Angle (ERA) for a given Julian Date according to IERS Technical Note No. 32.
        /// </summary>
        /// <param name="jd">Julian Date.</param>
        /// <returns>Earth Rotation Angle in radians.</returns>
        public static double EarthRotationAngle(double jd)
        {
            // IERS Technical Note No. 32
            double t = jd - 2451545.0;
            double f = jd % 1.0;

            double theta = 2 * Math.PI * (f + 0.7790572732640 + 0.00273781191135448 * t); // eq 14
            theta %= 2 * Math.PI;
            if (theta < 0) theta += 2 * Math.PI;

            return theta;
        }
    }
}
