using EphemSharp.Units;
using EphemSharp.Enums;
using System;
using EphemSharp.Data;
using EphemSharp.Data.vsop87;

namespace EphemSharp.Bodies
{
    public class Planet : CelestialBody
    {
        internal Func<AstroTime, Planet> EphemerisProvider { get; set; }
        public Distance EarthDistance { get; private set; }
        public Distance SunDistance { get; private set; }
        public Vector HeliocentricVector { get; private set; }
        public Angle AngularSize { get; private set; }
        public Angle PhaseAngle { get; private set; }
        public double Illumination { get; private set; }
        public double Magnitude { get; private set; }

        internal Planet(Distance ed, Distance sd, Angle ra, Angle dec, Angle size, Angle ph, double mag, Vector xyz)
        {
            EarthDistance = ed;
            SunDistance = sd;
            RightAscension = ra;
            Declination = dec;
            AngularSize = size;
            PhaseAngle = ph;
            Illumination = (1 + Math.Cos(ph.Radians)) / 2;
            Magnitude = mag;
            HeliocentricVector = xyz;
        }

        internal static Planet BuildPlanet(Planets name, XYZRHandler xyzr, AstroTime time, double radius)
        {
            var exyzr = Earth.XYZR(time); //earth coorditanates in space 
            var pxyzr = xyzr(time);

            //earth-planet coordinates
            Vector earthPlanetVector = pxyzr.xyz - exyzr.xyz;

            double delta = earthPlanetVector.Length();// earth-planet distance
            Distance r = pxyzr.r;     // planet-sun distance
            Distance R = exyzr.r;      // Sun-Earth distance


            var (ra, dec) = MathUtils.ToRaDec(earthPlanetVector.X, earthPlanetVector.Y, earthPlanetVector.Z);

            Distance earthD = new Distance(au: delta);

            Angle ph = MathUtils.CountPhaseAngle(r.AU, delta, R.AU);

            double mag = MagnitudeCounter.GetMagnitude(name, r.AU, delta, ph.GetDegrees(), ra, dec);

            var planet = new Planet(earthD, r, //distances
                new Angle(AngleType.Hours, ra), new Angle(AngleType.Degrees, dec), //radec
                new Angle(AngleType.Degrees, Math.Asin(radius / earthD.KM) * 2.0), //angular size
                ph, mag, //phase angle and magnitude
                pxyzr.xyz); //xyz about the earth


            planet.EphemerisProvider = delegate(AstroTime requestedTime)
            {
                return BuildPlanet(name, xyzr, requestedTime, radius);
            };
            return planet;

        }

        internal Planet AtTime(AstroTime time)
        {
            return EphemerisProvider == null ? this : EphemerisProvider(time);
        }

        internal delegate (Vector xyz, Distance r) XYZRHandler(AstroTime time);
    }
}
