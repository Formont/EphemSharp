using EphemSharp.Enums;
using EphemSharp.Units;
using System;

namespace EphemSharp
{
    internal class MathUtils
    {

        internal static Angle CountPhaseAngle(double r, double delta, double R)
        {
            double cosAlpha = (r * r + delta * delta - R * R) / (2 * r * delta);
            cosAlpha = Math.Max(-1.0, Math.Min(1.0, cosAlpha));

            double alphaRad = Math.Acos(cosAlpha);
            Angle phase = new Angle(AngleType.Degrees, alphaRad);

            return phase;
        }

        internal static (double ra, double dec) ToRaDec(double x, double y, double z)
        {
            double epsilon = 23.43928 * Math.PI / 180.0;

            double x_eq = x;
            double y_eq = y * Math.Cos(epsilon) - z * Math.Sin(epsilon);
            double z_eq = y * Math.Sin(epsilon) + z * Math.Cos(epsilon);

            double r = Math.Sqrt(x_eq * x_eq + y_eq * y_eq + z_eq * z_eq);
            double ra = Math.Atan2(y_eq, x_eq);
            if (ra < 0)
                ra += 2 * Math.PI;

            double dec = Math.Asin(z_eq / r);

            return (ra, dec);
        }
    }
}
