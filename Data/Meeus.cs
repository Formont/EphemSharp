using EphemSharp.Bodies;
using EphemSharp.Data.MeeusAlgorithms;
using EphemSharp.Enums;
using System;


namespace EphemSharp.Data
{
    public class Meeus
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
            double PlutoRKM = 1188.3;

            switch (planetName)
            {
                case Planets.Pluto:
                    return Planet.BuildPlanet(planetName, Pluto.XYZR, time, PlutoRKM);
                default:
                    throw new ArgumentException($"Planet {planetName} is not supported.");
            }
        }
    }
}
