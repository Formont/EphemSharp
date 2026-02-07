using EphemSharp.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemSharp
{
    public static class AstronomicalEvents
    {
        static bool IsAboveHorizon(Observer observer, CelestialBody body, DateTime time)
        {
            var obs = observer.Observe(body, time);
            return obs.Altitude.GetDegrees() > 0;
        }

        public static DateTime? FindRiseTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            bool? previous = null;
            DateTime? riseTime = null;

            for (var t = start; t <= end; t += TimeSpan.FromMinutes(2))
            {
                bool current = IsAboveHorizon(observer, body, t);

                if (previous != null && previous == false && current == true)
                {
                    riseTime = t;
                    break;
                }

                previous = current;
            }

            return riseTime;
        }
        public static DateTime? FindSetTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime current = start;
            bool wasAbove = false;

            while (current <= end)
            {
                var observed = observer.Observe(body, current);
                bool isAbove = observed.Altitude.GetDegrees() > 0;

                if (wasAbove && !isAbove)
                {
                    return current;
                }

                wasAbove = isAbove;
                current = current.Add(TimeSpan.FromMinutes(2));
            }

            return null; 
        }

        public static (DateTime? time, Angle? altitude) FindTransitTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime current = start;
            DateTime? maxTime = null;
            double maxAlt = double.MinValue;

            while (current <= end)
            {
                var observed = observer.Observe(body, current);
                double alt = observed.Altitude.GetDegrees();

                if (alt > maxAlt)
                {
                    maxAlt = alt;
                    maxTime = current;
                }

                current = current.Add(TimeSpan.FromMinutes(2));
            }

            if (maxTime.HasValue)
            {
                var alt = observer.Observe(body, maxTime.Value).Altitude;
                return (maxTime, alt);
            }

            return (null, null);
        }
    }
}
