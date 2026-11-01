using EphemSharp.Bodies;
using EphemSharp.Data.elp2000_82b;
using EphemSharp.Data.vsop87;
using EphemSharp.Enums;
using EphemSharp.Units;
using System;



namespace EphemSharp.Data
{
    public class ELP2000_82B
    {

        public static Planet GetMoon()
        {
            return GetMoon(new AstroTime(DateTime.UtcNow));
        }

        public static Planet GetMoon(AstroTime time)
        {
            return BuildMoon(time, Moon.XYZR);
        }

        static Planet BuildMoon(AstroTime time, Planet.XYZRHandler xyzr)
        {
            double MoonRKM = 1737.4;

            return Planet.BuildPlanet(Planets.Moon, xyzr, time, MoonRKM);
        }
    }
}
