using EphemSharp.Bodies;
using EphemSharp.Data.vsop87;
using EphemSharp.Enums;
using EphemSharp.Units;
using System;

namespace EphemSharp.Data
{
    public static class VSOP87
    {
        public static Planet GetPlanet(Planets planetName)
        {
            return GetPlanet(planetName, new AstroTime(DateTime.UtcNow));
        }

        public static Planet GetPlanet(Planets planetName, DateTime dateTime)
        {
            return GetPlanet(planetName, new AstroTime(dateTime));
        }
        public static Planet GetPlanet(Planets planetName, AstroTime time)
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
            //double MoonRKM = 1737.4;

            switch (planetName)
            {
                //https://www.neoprogrammics.com/vsop87/source_code_generator_tool/
                case Planets.Mercury:
                    return Planet.BuildPlanet(planetName, Mercury.XYZR, time, MercuryRKM);
                case Planets.Venus:
                    return Planet.BuildPlanet(planetName, Venus.XYZR, time, VenusRKM);
                case Planets.Mars:
                    return Planet.BuildPlanet(planetName, Mars.XYZR, time, MarsRKM);
                case Planets.Jupiter:
                    return Planet.BuildPlanet(planetName, Jupiter.XYZR, time, JupiterRKM);
                case Planets.Saturn:
                    return Planet.BuildPlanet(planetName, Saturn.XYZR, time, SaturnRKM);
                case Planets.Uranus:
                    return Planet.BuildPlanet(planetName, Uranus.XYZR, time, UranusRKM);
                case Planets.Neptune:
                    return Planet.BuildPlanet(planetName, Neptune.XYZR, time, NeptuneRKM);
                case Planets.Earth:
                    return Planet.BuildPlanet(planetName, Earth.XYZR, time, EarthRKM);
                case Planets.Sun:
                    return Planet.BuildPlanet(planetName, Sun.XYZR, time, SunRKM);
                default:
                    throw new ArgumentException($"Planet {planetName} is not supported.");  
            }
        }
    }
}
