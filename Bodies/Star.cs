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
                double D = 1 / Math.Tan(ParallaxMas / 1000) * 206000;
                Distance = new Distance(D);
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
            if (ra.Degrees < 0 || ra.Degrees >= 24)
                throw new ArgumentOutOfRangeException(nameof(ra.Degrees));

            if (dec.Degrees < -90 || dec.Degrees > 90)
                throw new ArgumentOutOfRangeException(nameof(dec.Degrees));
        }

        public static Star FromHIP(int number, Dictionary<string, Star> HIPstars)
        {   
            return HIPstars[number.ToString()];
        }
    }
}
