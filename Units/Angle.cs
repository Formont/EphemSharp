using System;
using EphemSharp.Enums;

namespace EphemSharp.Units
{
    public struct Angle
    {
        public int Degrees { get; private set; }
        public int Arcmin { get; private set; }
        public double Arcsec { get; private set; }

        public double Radians { get; private set; }
        public AngleType Kind { get; private set; }

        private const double TAU = 2 * Math.PI;

        public Angle(AngleType kind, int degrees, int arcmin = 0, double arcsec = 0)
        {
            Kind = kind;
            Degrees = degrees;
            Arcmin = Math.Abs(arcmin);
            Arcsec = Math.Abs(arcsec);

            int sign = Math.Sign(degrees != 0 ? degrees : (arcmin != 0 ? arcmin : arcsec));
            if (sign == 0) sign = 1;

            double total = Math.Abs(degrees) + Math.Abs(arcmin) / 60.0 + Math.Abs(arcsec) / 3600.0;
            total *= sign;

            Radians = kind == AngleType.Degrees
                ? total / 360.0 * TAU
                : total / 24.0 * TAU;
        }

        public Angle(AngleType kind, double radians)
        {
            Kind = kind;
            Radians = radians;

            double total = kind == AngleType.Degrees
                ? radians * 360.0 / TAU
                : radians * 24.0 / TAU;

            double absTotal = Math.Abs(total);
            int sign = Math.Sign(total);
            if (sign == 0) sign = 1;

            int deg = (int)absTotal;
            double mins = (absTotal - deg) * 60.0;
            int min = (int)mins;
            double sec = (mins - min) * 60.0;

            Degrees = sign * deg;
            Arcmin = min;
            Arcsec = sec;
        }

        public static Angle FromDouble(double value, AngleType kind)
        {
            double absValue = Math.Abs(value);
            int deg = (int)absValue;
            double mins = (absValue - deg) * 60.0;
            int min = (int)mins;
            double sec = (mins - min) * 60.0;

            int sign = Math.Sign(value);
            if (sign < 0)
            {
                if (deg != 0) deg = -deg;
                else if (min != 0) min = -min;
                else sec = -sec;
            }
            return new Angle(kind, deg, min, sec);
        }

        public double ToArcMinutes() => GetValue() * 60.0;
        public double ToArcSeconds() => GetValue() * 3600.0;
        public double ToMas() => GetValue() * 3600000.0;

        public double GetHours() => Radians * 24.0 / TAU;
        public double GetDegrees() => Radians * 360.0 / TAU;

        private double GetValue()
        {
            return Kind == AngleType.Degrees ? GetDegrees() : GetHours();
        }

        public override string ToString()
        {
            bool isNegative = Radians < 0;
            string signStr = (isNegative && Degrees == 0) ? "-" : "";
            return Kind == AngleType.Degrees
                ? $"{signStr}{Degrees}° {Arcmin}' {Arcsec}\""
                : $"{signStr}{Degrees}h {Arcmin}m {Arcsec}s";
        }

        public static explicit operator Angle(double value)
        {
            // Значение будет интерпретироваться как "градусы" по умолчанию
            return FromDouble(value, AngleType.Degrees);
        }

        public static explicit operator double(Angle angle)
        {
            return angle.Kind == AngleType.Degrees ? angle.GetDegrees() : angle.GetHours();
        }

        public static implicit operator string(Angle angle) => angle.ToString();
    }
}
