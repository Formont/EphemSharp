using EphemSharp.Units;
using EphemSharp.Data;
using EphemSharp.Bodies;
using EphemSharp.Data.vsop87;
using EphemSharp.Data.elp2000_82b;
using EphemSharp.Enums;
using System;


namespace EphemSharp
{
    public static class AstronomicalEvents
    {
        static bool IsAboveHorizon(Observer observer, CelestialBody body, DateTime utcTime)
        {
            AstroTime time = new AstroTime(utcTime);
            Planet planet = body as Planet;
            CelestialBody bodyAtTime = planet == null ? body : planet.AtTime(time);
            var obs = observer.Observe(bodyAtTime, time);
            return obs.Altitude.GetDegrees() > 0;
        }

        public static DateTime? FindRiseTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime? crossing = FindHorizonCrossing(observer, body, start, end, true);
            return crossing.HasValue ? (DateTime?)observer.ConvertToLocal(crossing.Value) : null;
        }

        public static DateTime? FindSetTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            DateTime? crossing = FindHorizonCrossing(observer, body, start, end, false);
            return crossing.HasValue ? (DateTime?)observer.ConvertToLocal(crossing.Value) : null;
        }

        private static DateTime? FindHorizonCrossing(Observer observer, CelestialBody body, DateTime start, DateTime end, bool rising)
        {
            if (observer == null) throw new ArgumentNullException("observer");
            if (body == null) throw new ArgumentNullException("body");
            DateTime startUtc = observer.ConvertToUtc(start);
            DateTime endUtc = observer.ConvertToUtc(end);
            if (endUtc < startUtc) throw new ArgumentOutOfRangeException("end", "End time must not be earlier than start time.");

            DateTime previousTime = startUtc;
            bool previousAbove = IsAboveHorizon(observer, body, previousTime);
            TimeSpan step = TimeSpan.FromMinutes(2);
            while (previousTime < endUtc)
            {
                DateTime currentTime = previousTime + step;
                if (currentTime > endUtc) currentTime = endUtc;
                bool currentAbove = IsAboveHorizon(observer, body, currentTime);
                if (previousAbove != currentAbove && currentAbove == rising)
                {
                    // Refine the two-minute bracket to approximately one second.
                    DateTime low = previousTime;
                    DateTime high = currentTime;
                    while ((high - low).TotalSeconds > 1.0)
                    {
                        DateTime middle = low + TimeSpan.FromTicks((high - low).Ticks / 2);
                        if (IsAboveHorizon(observer, body, middle) == previousAbove) low = middle;
                        else high = middle;
                    }
                    return high;
                }
                previousTime = currentTime;
                previousAbove = currentAbove;
            }
            return null;
        }

        public static (DateTime? time, Angle? altitude) FindTransitTime(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            if (observer == null) throw new ArgumentNullException("observer");
            if (body == null) throw new ArgumentNullException("body");
            DateTime startUtc = observer.ConvertToUtc(start);
            DateTime endUtc = observer.ConvertToUtc(end);
            if (endUtc < startUtc) throw new ArgumentOutOfRangeException("end", "End time must not be earlier than start time.");

            DateTime currentUtc = startUtc;
            DateTime? maxTimeUtc = null;
            double maxAlt = double.MinValue;

            while (currentUtc <= endUtc)
            {
                double alt = GetAltitude(observer, body, currentUtc);

                if (alt > maxAlt)
                {
                    maxAlt = alt;
                    maxTimeUtc = currentUtc;
                }

                currentUtc = currentUtc.Add(TimeSpan.FromMinutes(2));
            }

            if (maxTimeUtc.HasValue)
            {
                DateTime low = maxTimeUtc.Value - TimeSpan.FromMinutes(2);
                DateTime high = maxTimeUtc.Value + TimeSpan.FromMinutes(2);
                if (low < startUtc) low = startUtc;
                if (high > endUtc) high = endUtc;
                DateTime transitUtc = RefineAltitudeMaximum(observer, body, low, high);
                double altitudeDegrees = GetAltitude(observer, body, transitUtc);
                var altitude = new Angle(AngleType.Degrees, altitudeDegrees * Math.PI / 180.0);
                return (observer.ConvertToLocal(transitUtc), altitude);
            }

            return (null, null);
        }

        private static DateTime RefineAltitudeMaximum(Observer observer, CelestialBody body, DateTime start, DateTime end)
        {
            long left = start.Ticks;
            long right = end.Ticks;
            const double inversePhi = 0.6180339887498948482;
            long x1 = right - (long)((right - left) * inversePhi);
            long x2 = left + (long)((right - left) * inversePhi);
            double y1 = GetAltitude(observer, body, new DateTime(x1, start.Kind));
            double y2 = GetAltitude(observer, body, new DateTime(x2, start.Kind));

            while (right - left > TimeSpan.TicksPerSecond)
            {
                if (y1 < y2)
                {
                    left = x1;
                    x1 = x2;
                    y1 = y2;
                    x2 = left + (long)((right - left) * inversePhi);
                    y2 = GetAltitude(observer, body, new DateTime(x2, start.Kind));
                }
                else
                {
                    right = x2;
                    x2 = x1;
                    y2 = y1;
                    x1 = right - (long)((right - left) * inversePhi);
                    y1 = GetAltitude(observer, body, new DateTime(x1, start.Kind));
                }
            }
            return new DateTime(left + (right - left) / 2, start.Kind);
        }

        private static double GetAltitude(Observer observer, CelestialBody body, DateTime utcTime)
        {
            AstroTime time = new AstroTime(utcTime);
            Planet planet = body as Planet;
            CelestialBody bodyAtTime = planet == null ? body : planet.AtTime(time);
            return observer.Observe(bodyAtTime, time).Altitude.GetDegrees();
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
            AstroTime time = new AstroTime(utcTime);

            var earthPos = Earth.XYZR(time);
            double l_sun = Math.Atan2(-earthPos.xyz.Y, -earthPos.xyz.X);
            if (l_sun < 0) l_sun += 2 * Math.PI;

            var moon = ELP2000_82B.GetMoon(time);
            Vector moonGeocentric = moon.HeliocentricVector - earthPos.xyz;
            double l_moon = Math.Atan2(moonGeocentric.Y, moonGeocentric.X);
            if (l_moon < 0) l_moon += 2 * Math.PI;

            double diffDeg = (l_moon - l_sun) * 180.0 / Math.PI;
            diffDeg = (diffDeg % 360 + 360) % 360;

            int phaseIndex = (int)Math.Floor((diffDeg + 22.5) / 45.0) % 8;
            return (MoonPhase)phaseIndex;
        }
    }
}
