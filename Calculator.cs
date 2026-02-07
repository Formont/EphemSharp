using EphemSharp.Enums;
using EphemSharp.Units;
using System;


namespace EphemSharp
{
    public static class Calculator
    {
        public static Angle AngularDistance(CelestialBody a, CelestialBody b)
        {
            double ra1Rad = a.RightAscension.Radians;
            double ra2Rad = b.RightAscension.Radians;
            double dec1Rad = a.Declination.Radians;
            double dec2Rad = b.Declination.Radians;

            double cosTheta = Math.Sin(dec1Rad) * Math.Sin(dec2Rad) +
                              Math.Cos(dec1Rad) * Math.Cos(dec2Rad) * Math.Cos(ra1Rad - ra2Rad);

            cosTheta = Clamp(cosTheta, -1.0, 1.0);

            double theta = Math.Acos(cosTheta);
            return new Angle(AngleType.Degrees, theta);
        }
        static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
