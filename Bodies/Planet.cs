using EphemSharp.Units;
using EphemSharp.Enums;
using EphemSharp.Utils;
using System;
using EphemSharp.Data;

namespace EphemSharp.Bodies
{
    public class Planet : CelestialBody
    {
        public Distance EarthDistance { get; private set; }
        public Distance SunDistance { get; private set; }
        public Vector XYZ { get; private set; }
        public Angle AngularSize { get; private set; }
        public double Illumination { get; private set; }
        public double Magnitude { get; private set; }

        Planet(Distance ed, Distance sd, Angle ra, Angle dec, Angle size, Angle ph, double mag, Vector xyz)
        {
            EarthDistance = ed;
            SunDistance = sd;
            RightAscension = ra;
            Declination = dec;
            AngularSize = size;
            Illumination = (1 + Math.Cos(ph.Radians)) / 2;
            Magnitude = mag;
            XYZ = xyz;
        }

        public static Planet GetPlanet(Planets planetName)
        {
            return GetPlanet(planetName, Time.ToJulianDate(DateTime.Now));
        }

        public static Planet GetPlanet(Planets planetName, DateTime dateTime)
        {
            return GetPlanet(planetName, Time.ToJulianDate(dateTime));
        }
        public static Planet GetPlanet(Planets planetName, double jd)
        {

            //https://ssd.jpl.nasa.gov/planets/phys_par.html

            //radiuses in km
            double SunRKM = 695700.0;
            double MercuryRKM = 2440.53;
            double VenusRKM = 6051.8;
            double MarsRKM = 3396.19;
            double JupiterRKM = 71492.0;
            double SaturnRKM = 60268.0;
            double UranusRKM = 25559.0;
            double NeptuneRKM = 24764.0;
            double EarthRKM = 6378.1366;

            switch (planetName)
            {
                //https://www.neoprogrammics.com/vsop87/source_code_generator_tool/
                case Planets.Mercury:
                    return BuildPlanet(planetName ,Mercury.XYZR, jd, MercuryRKM);
                case Planets.Venus:
                    return BuildPlanet(planetName, Venus.XYZR, jd, VenusRKM);
                case Planets.Mars:
                    return BuildPlanet(planetName, Mars.XYZR, jd, MarsRKM);
                case Planets.Jupiter:
                    return BuildPlanet(planetName, Jupiter.XYZR, jd, JupiterRKM);
                case Planets.Saturn:
                    return BuildPlanet(planetName, Saturn.XYZR, jd, SaturnRKM);
                case Planets.Uranus:
                    return BuildPlanet(planetName, Uranus.XYZR, jd, UranusRKM);
                case Planets.Neptune:
                    return BuildPlanet(planetName, Neptune.XYZR, jd, NeptuneRKM);
                case Planets.Earth:
                    return BuildPlanet(planetName, Earth.XYZR, jd, EarthRKM);
                case Planets.Sun:
                    return BuildPlanet(planetName, Sun.XYZR, jd, SunRKM);
            }
            return null;
        }

        static Planet BuildPlanet(Planets name, XYZRHandler xyzr, double jd, double radius)
        {
            var exyzr = Earth.XYZR(jd); //earth coorditanates in space 
            var pxyzr = xyzr(jd);

            //earth-planet coordinates
            Vector earthPlanetVector = pxyzr.xyz - exyzr.xyz;

            double delta = earthPlanetVector.Length();// earth-planet distance
            Distance r = pxyzr.r;     // planet-sun distance
            Distance R = exyzr.r;      // Sun-Earth distance


            var (ra, dec) = ToRaDec(earthPlanetVector.X, earthPlanetVector.Y, earthPlanetVector.Z);

            Distance earthD = new Distance(au: delta);

            Angle ph = CountPhaseAngle(r.AU, delta, R.AU);

            double mag = MagnitudeCounter.GetMagnitude(name, r.AU, delta, ph.Degrees);

            var planet = new Planet(earthD, r, //distances
                new Angle(AngleType.Hours, ra), new Angle(AngleType.Degrees, dec), //radec
                new Angle(AngleType.Degrees, Math.Asin(radius / earthD.KM) * 2.0), //angular size
                ph, mag, //phase angle and magnitude
                pxyzr.xyz); //xyz about the earth


            return planet;

        }
        static Angle CountPhaseAngle(double r, double delta, double R)
        {
            double cosAlpha = (r * r + delta * delta - R * R) / (2 * r * delta);
            cosAlpha = Math.Max(-1.0, Math.Min(1.0, cosAlpha));

            double alphaRad = Math.Acos(cosAlpha);
            Angle phase = new Angle(AngleType.Degrees, alphaRad);

            return phase;
        }

        static (double ra, double dec) ToRaDec(double x, double y, double z)
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
        delegate (Vector xyz, Distance r) XYZRHandler(double jd);
    }
}
