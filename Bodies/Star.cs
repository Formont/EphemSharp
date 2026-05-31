using System;
using System.Collections.Generic;
using EphemSharp.Enums;
using EphemSharp.Units;

namespace EphemSharp.Bodies
{
    public class Star : CelestialBody
    {
        private double parallaxMas;
        public double Magnitude { get; set; }
        public Distance Distance { get; private set; }
        public double ParallaxMas
        {
            get { return parallaxMas; }
            set
            {
                parallaxMas = value;
                if (parallaxMas <= 0)
                {
                    Distance = new Distance(au: double.PositiveInfinity);
                }
                else
                {
                    double radians = (parallaxMas / 1000.0) / 3600.0 * Math.PI / 180.0;
                    double D = 1.0 / Math.Tan(radians);
                    Distance = new Distance(D);
                }
            }
        }

        public double RaMasPerYear { get; set; }
        public double DecMasPerYear { get; set; }
        public Star(double raHours, double decDegrees)
        {
            if (raHours < 0 || raHours >= 24)
                throw new ArgumentOutOfRangeException(
                    nameof(raHours),
                    "Right Ascension must be given in HOURS (0 ≤ RA < 24)."
                );

            if (decDegrees < -90 || decDegrees > 90)
                throw new ArgumentOutOfRangeException(
                    nameof(decDegrees),
                    "Declination must be given in DEGREES (-90 ≤ Dec ≤ +90)."
                );

            double raRadians = raHours * Math.PI / 12.0;   // 24h → 2π
            double decRadians = decDegrees * Math.PI / 180.0;

            RightAscension = new Angle(AngleType.Hours, raRadians);
            Declination = new Angle(AngleType.Degrees, decRadians);
        }

        public Star(Angle ra, Angle dec)
        {
            RightAscension = ra;
            Declination = dec;
            Magnitude = 0;
            double raHours = ra.GetHours();
            if (raHours < 0 || raHours >= 24)
                throw new ArgumentOutOfRangeException(nameof(ra), "Right Ascension must be between 0 and 24 hours.");

            double decDegrees = dec.GetDegrees();
            if (decDegrees < -90 || decDegrees > 90)
                throw new ArgumentOutOfRangeException(nameof(dec), "Declination must be between -90 and 90 degrees.");
        }

        public static Star FromHIP(int number, Dictionary<string, Star> HIPstars)
        {   
            return HIPstars[number.ToString()];
        }
    }
}
