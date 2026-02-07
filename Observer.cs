using System;
using EphemSharp.Utils;
using EphemSharp.Units;
using EphemSharp.Enums;

namespace EphemSharp
{
namespace EphemSharp
{
    public class Observer
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
        double Elevation { get; set; }  
        public Observer() 
        { 
            Latitude = 0;
            Longitude= 0;
            Elevation = 0;
        }
        public Observer(double latitude, double longitude, double elevation = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }
        public ObservedObject Observe(CelestialBody body)
        {
            return Observe(body, Time.ToJulianDate(DateTime.UtcNow));
        }
        public ObservedObject Observe(CelestialBody body, DateTime utc)
        {
            return Observe(body, Time.ToJulianDate(utc));
        }
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
            return new double[] {a, az, H };
        }

        public static double GreenwichMeanSiderealTime(double jd)
        {
            // "Expressions for IAU 2000 precession quantities" N. Capitaine1,P.T.Wallace2, and J. Chapront
            double t = (jd - 2451545.0) / 36525.0;

            double gmst = EarthRotationAngle(jd) + (0.014506 + 4612.156534 * t + 1.3915817 * t * t - 0.00000044 * t * t * t - 0.000029956 * t * t * t * t - 0.0000000368 * t * t * t * t * t) / 60.0 / 60.0 * Math.PI / 180.0;  // eq 42
            gmst %= 2 * Math.PI;
            if (gmst < 0) gmst += 2 * Math.PI;

            return gmst;
        }

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

    public class Observer
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
        double Elevation { get; set; }  
        public Observer() 
        { 
            Latitude = 0;
            Longitude= 0;
            Elevation = 0;
        }
        public Observer(double latitude, double longitude, double elevation = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }
        public ObservedObject Observe(CelestialBody body)
        {
            return Observe(body, Time.ToJulianDate(DateTime.UtcNow));
        }
        public ObservedObject Observe(CelestialBody body, DateTime utc)
        {
            return Observe(body, Time.ToJulianDate(utc));
        }
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
            return new double[] {a, az, H };
        }

        public static double GreenwichMeanSiderealTime(double jd)
        {
            // "Expressions for IAU 2000 precession quantities" N. Capitaine1,P.T.Wallace2, and J. Chapront
            double t = (jd - 2451545.0) / 36525.0;

            double gmst = EarthRotationAngle(jd) + (0.014506 + 4612.156534 * t + 1.3915817 * t * t - 0.00000044 * t * t * t - 0.000029956 * t * t * t * t - 0.0000000368 * t * t * t * t * t) / 60.0 / 60.0 * Math.PI / 180.0;  // eq 42
            gmst %= 2 * Math.PI;
            if (gmst < 0) gmst += 2 * Math.PI;

            return gmst;
        }

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
