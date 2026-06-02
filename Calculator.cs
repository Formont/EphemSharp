using EphemSharp.Enums;
using EphemSharp.Units;
using EphemSharp.Bodies;
using System;
using System.Collections.Generic;


namespace EphemSharp
{
    public static class Calculator
    {
        /// <summary>
        /// Calculates the angular distance between two celestial bodies on the celestial sphere.
        /// </summary>
        /// <param name="a">The first celestial body.</param>
        /// <param name="b">The second celestial body.</param>
        /// <returns>An <see cref="Angle"/> representing the angular distance between the two bodies.</returns>
        public static Angle AngularDistance(CelestialBody a, CelestialBody b)
        {
            double ra1Rad = a.RightAscension.Radians;
            double ra2Rad = b.RightAscension.Radians;
            double dec1Rad = a.Declination.Radians;
            double dec2Rad = b.Declination.Radians;

            double cosTheta = Math.Sin(dec1Rad) * Math.Sin(dec2Rad) +
                              Math.Cos(dec1Rad) * Math.Cos(dec2Rad) * Math.Cos(ra1Rad - ra2Rad);

            cosTheta = Clamp(cosTheta, -1.0, 1.0);

            double theta = Math.Acos(cosTheta);
            return new Angle(AngleType.Degrees, theta);
        }

        /// <summary>
        /// Clamps a value between a minimum and a maximum boundary.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum boundary.</param>
        /// <param name="max">The maximum boundary.</param>
        /// <returns>The clamped value.</returns>
        static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Calculates the times for astronomical/nautical/civil twilight transitions, sunrise, and sunset on a given date.
        /// </summary>
        /// <param name="obs">The observer details with geographic coordinates.</param>
        /// <param name="dateTime">The date for which twilight transitions are calculated.</param>
        /// <returns>A dictionary containing twilight names (keys) and their corresponding transition times (values).</returns>
        public static Dictionary<string, DateTime> CountTwilights(Observer obs, DateTime dateTime)
        {
            var result = new Dictionary<string, DateTime>();

            DateTime localStart = dateTime.Date;
            DateTime localEnd = localStart.AddDays(1);

            DateTime startUtc = obs.ConvertToUtc(localStart);
            DateTime endUtc = obs.ConvertToUtc(localEnd);

            double[] thresholds = { -18.0, -12.0, -6.0, -0.833 };
            string[] riseNames = { "Astronomical Dawn", "Nautical Dawn", "Civil Dawn", "Sunrise" };
            string[] setNames = { "Astronomical Dusk", "Nautical Dusk", "Civil Dusk", "Sunset" };

            DateTime currentUtc = startUtc;
            double prevAlt = SunAltitude(obs, Utils.Time.ToJulianDate(currentUtc));

            while (currentUtc <= endUtc)
            {
                currentUtc = currentUtc.AddMinutes(1);
                double alt = SunAltitude(obs, Utils.Time.ToJulianDate(currentUtc));

                for (int i = 0; i < thresholds.Length; i++)
                {
                    double thresh = thresholds[i];
                    // Check if it crossed the threshold rising
                    if (prevAlt < thresh && alt >= thresh)
                    {
                        string name = riseNames[i];
                        if (!result.ContainsKey(name)) result[name] = obs.ConvertToLocal(currentUtc);
                    }
                    // Check if it crossed the threshold setting
                    if (prevAlt >= thresh && alt < thresh)
                    {
                        string name = setNames[i];
                        if (!result.ContainsKey(name)) result[name] = obs.ConvertToLocal(currentUtc);
                    }
                }

                prevAlt = alt;
            }
            return result;
        }

        /// <summary>
        /// Calculates the precise altitude of the Sun above the horizon for a given observer and Julian Date.
        /// </summary>
        /// <param name="obs">The observer with geographic coordinates.</param>
        /// <param name="jd">The Julian Date at which the calculation is performed.</param>
        /// <returns>The angular altitude of the Sun in degrees.</returns>
        static double SunAltitude(Observer obs, double jd)
        {
            var sun = Planet.GetPlanet(Planets.Sun, jd);
            var observed = obs.Observe(sun, jd);
            return observed.Altitude.GetDegrees();
        }
    }
}
