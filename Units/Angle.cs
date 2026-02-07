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
            Arcmin = arcmin;
            Arcsec = arcsec;

            int sign = Math.Sign(degrees != 0 ? degrees : (Arcmin != 0 ? Arcmin : arcsec));
            double total = Math.Abs(degrees) + Arcmin / 60.0 + arcsec / 3600.0;
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

            Degrees = (int)total;
            double mins = (total - Degrees) * 60.0;
            Arcmin = (int)Math.Abs(mins);
            Arcsec = Math.Abs((mins - Arcmin) * 60.0);
        }

        public static Angle FromDouble(double value, AngleType kind)
        {
            int deg = (int)value;
            double mins = (value - deg) * 60.0;
            int min = (int)Math.Abs(mins);
            double sec = Math.Abs((mins - min) * 60.0);
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
            return Kind == AngleType.Degrees
                ? $"{Degrees}° {Arcmin}' {Arcsec}\""
                : $"{Degrees}h {Arcmin}m {Arcsec}s";
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
