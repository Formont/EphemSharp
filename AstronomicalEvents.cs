using EphemSharp.Units;
using EphemSharp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemSharp
{
    public static class AstronomicalEvents
    {
        static bool IsAboveHorizon(Observer observer, CelestialBody body, DateTime utcTime)
        {
            var obs = observer.Observe(body, Utils.Time.ToJulianDate(utcTime));
            return obs.Altitude.GetDegrees() > 0;
        }

        public static DateTime? FindRiseTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime startUtc = observer.ConvertToUtc(start);
            DateTime endUtc = observer.ConvertToUtc(end);

            bool? previous = null;
            DateTime? riseTimeUtc = null;

            for (var tUtc = startUtc; tUtc <= endUtc; tUtc += TimeSpan.FromMinutes(2))
            {
                bool current = IsAboveHorizon(observer, body, tUtc);

                if (previous != null && previous == false && current == true)
                {
                    riseTimeUtc = tUtc;
                    break;
                }

                previous = current;
            }

            if (riseTimeUtc.HasValue)
            {
                return observer.ConvertToLocal(riseTimeUtc.Value);
            }

            return null;
        }
        public static DateTime? FindSetTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime startUtc = observer.ConvertToUtc(start);
            DateTime endUtc = observer.ConvertToUtc(end);

            DateTime currentUtc = startUtc;
            bool wasAbove = false;

            while (currentUtc <= endUtc)
            {
                var observed = observer.Observe(body, Utils.Time.ToJulianDate(currentUtc));
                bool isAbove = observed.Altitude.GetDegrees() > 0;

                if (wasAbove && !isAbove)
                {
                    return observer.ConvertToLocal(currentUtc);
                }

                wasAbove = isAbove;
                currentUtc = currentUtc.Add(TimeSpan.FromMinutes(2));
            }

            return null; 
        }

        public static (DateTime? time, Angle? altitude) FindTransitTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime startUtc = observer.ConvertToUtc(start);
            DateTime endUtc = observer.ConvertToUtc(end);

            DateTime currentUtc = startUtc;
            DateTime? maxTimeUtc = null;
            double maxAlt = double.MinValue;

            while (currentUtc <= endUtc)
            {
                var observed = observer.Observe(body, Utils.Time.ToJulianDate(currentUtc));
                double alt = observed.Altitude.GetDegrees();

                if (alt > maxAlt)
                {
                    maxAlt = alt;
                    maxTimeUtc = currentUtc;
                }

                currentUtc = currentUtc.Add(TimeSpan.FromMinutes(2));
            }

            if (maxTimeUtc.HasValue)
            {
                var alt = observer.Observe(body, Utils.Time.ToJulianDate(maxTimeUtc.Value)).Altitude;
                return (observer.ConvertToLocal(maxTimeUtc.Value), alt);
            }

            return (null, null);
        }

        /// <summary>
        /// Gets the Moon phase at a specific local date and time of the observer.
        /// </summary>
        /// <param name="observer">The observer with geographical coordinates and timezone.</param>
        /// <param name="localTime">The local date and time of the observer.</param>
        /// <returns>The calculated <see cref="MoonPhase"/>.</returns>
        public static MoonPhase GetMoonPhase(Observer observer, DateTime localTime)
        {
            DateTime utcTime = observer.ConvertToUtc(localTime);
            double jd = Utils.Time.ToJulianDate(utcTime);

            var earthPos = Bodies.Earth.XYZR(jd);
            double l_sun = Math.Atan2(-earthPos.xyz.Y, -earthPos.xyz.X);
            if (l_sun < 0) l_sun += 2 * Math.PI;

            var moon = Bodies.Planet.GetPlanet(Planets.Moon, jd);
            Vector moonGeocentric = moon.XYZ - earthPos.xyz;
            double l_moon = Math.Atan2(moonGeocentric.Y, moonGeocentric.X);
            if (l_moon < 0) l_moon += 2 * Math.PI;

            double diffDeg = (l_moon - l_sun) * 180.0 / Math.PI;
            diffDeg = (diffDeg % 360 + 360) % 360;

            int phaseIndex = (int)Math.Floor((diffDeg + 22.5) / 45.0) % 8;
            return (MoonPhase)phaseIndex;
        }
    }
}
