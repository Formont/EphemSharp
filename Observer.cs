using System;
using EphemSharp.Utils;
using EphemSharp.Units;
using EphemSharp.Enums;
using EphemSharp.Bodies;

namespace EphemSharp
{
    /// <summary>
    /// Represents a geographic observer on Earth, used to compute the visible horizontal coordinates of celestial bodies.
    /// </summary>
    public class Observer
    {
        /// <summary>
        /// Geographic latitude of the observer in degrees (positive is North, negative is South).
        /// </summary>
        double Latitude { get; set; }

        /// <summary>
        /// Geographic longitude of the observer in degrees (positive is East, negative is West).
        /// </summary>
        double Longitude { get; set; }

        /// <summary>
        /// Elevation of the observer above sea level in meters.
        /// </summary>
        double Elevation { get; set; }  

        private TimeZoneInfo _timeZone;

        /// <summary>
        /// Gets or sets the timezone for the observer. If not explicitly set, a custom timezone is created based on the longitude-derived offset.
        /// </summary>
        public TimeZoneInfo TimeZone
        {
            get
            {
                if (_timeZone != null)
                {
                    return _timeZone;
                }
                double offsetHours = Math.Round(Longitude / 15.0);
                offsetHours = Math.Max(-14, Math.Min(14, offsetHours));
                TimeSpan offset = TimeSpan.FromHours(offsetHours);
                return TimeZoneInfo.CreateCustomTimeZone($"Offset_{offsetHours}", offset, $"UTC{offsetHours:+#;-#;+0}", $"UTC{offsetHours:+#;-#;+0}");
            }
            set
            {
                _timeZone = value;
            }
        }

        /// <summary>
        /// Gets the timezone offset in hours for the current timezone.
        /// </summary>
        public double TimeZoneOffset => TimeZone.BaseUtcOffset.TotalHours;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class at 0 latitude and 0 longitude.
        /// </summary>
        public Observer() 
        { 
            Latitude = 0;
            Longitude = 0;
            Elevation = 0;
            _timeZone = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class with specific geographic coordinates.
        /// </summary>
        /// <param name="latitude">The geographic latitude in degrees.</param>
        /// <param name="longitude">The geographic longitude in degrees.</param>
        /// <param name="elevation">The elevation above sea level in meters (defaults to 0).</param>
        /// <param name="timeZone">The optional observer's timezone.</param>
        public Observer(double latitude, double longitude, double elevation = 0, TimeZoneInfo timeZone = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
            _timeZone = timeZone;
        }

        /// <summary>
        /// Observes a celestial body at the current UTC time.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body)
        {
            return Observe(body, Time.ToJulianDate(DateTime.UtcNow));
        }

        /// <summary>
        /// Observes a celestial body at a specific local date and time of the observer.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <param name="localTime">The local date and time of the observer.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body, DateTime localTime)
        {
            DateTime utcTime = ConvertToUtc(localTime);
            return Observe(body, Time.ToJulianDate(utcTime));
        }

        /// <summary>
        /// Observes a celestial body at a specific Julian Date.
        /// </summary>
        /// <param name="body">The celestial body to observe.</param>
        /// <param name="jd">The Julian Date.</param>
        /// <returns>An <see cref="ObservedObject"/> containing the calculated altitude, azimuth, and hour angle.</returns>
        public ObservedObject Observe(CelestialBody body, double jd)
        {
            double raTopo, decTopo;
            Distance distTopo;
            GetTopocentricCoords(body, jd, out raTopo, out decTopo, out distTopo);

            double[] azah = RaDecToAltAz(raTopo,
                decTopo,
                ((Angle)this.Latitude).Radians,
                ((Angle)this.Longitude).Radians,
                jd
            );

            Angle alt = new Angle(AngleType.Degrees, azah[0]);
            Angle az = new Angle(AngleType.Degrees, azah[1]);
            Angle h = new Angle(AngleType.Hours, azah[2]);
            Angle ra = new Angle(AngleType.Hours, raTopo);
            Angle dec = new Angle(AngleType.Degrees, decTopo);
            return new ObservedObject(alt, az, h, ra, dec, distTopo);
        }

        private void GetTopocentricCoords(CelestialBody body, double jd, out double raTopo, out double decTopo, out Distance distTopo)
        {
            Distance geoDistance = null;
            if (body is Planet planet)
            {
                geoDistance = planet.EarthDistance;
            }
            else if (body is Star star)
            {
                geoDistance = star.Distance;
            }

            if (geoDistance == null || double.IsInfinity(geoDistance.AU) || double.IsNaN(geoDistance.AU))
            {
                raTopo = body.RightAscension.Radians;
                decTopo = body.Declination.Radians;
                distTopo = geoDistance ?? new Distance(au: double.PositiveInfinity);
                return;
            }

            double ra = body.RightAscension.Radians;
            double dec = body.Declination.Radians;
            double delta = geoDistance.AU;

            double lat = ((Angle)this.Latitude).Radians;
            double lon = ((Angle)this.Longitude).Radians;

            double gmst = GreenwichMeanSiderealTime(jd);
            double lst = (gmst + lon) % (2 * Math.PI);

            // Standard geodetic to geocentric coordinates conversion (Meeus AA, p.82)
            double f = 1.0 / 298.25642;
            double e2 = 2.0 * f - f * f;
            double sinLat = Math.Sin(lat);
            double C = 1.0 / Math.Sqrt(1.0 - e2 * sinLat * sinLat);

            double h_m = this.Elevation;
            double earthRadiusEqKM = 6378.1366;
            double earthRadiusEqMeters = earthRadiusEqKM * 1000.0;

            double rhoCosPhiP = (C + h_m / earthRadiusEqMeters) * Math.Cos(lat);
            double rhoSinPhiP = (C * (1.0 - e2) + h_m / earthRadiusEqMeters) * sinLat;

            // Convert observer's geocentric position vector to AU
            double earthRadiusEqAU = earthRadiusEqKM / Distance.AU_KM;
            double x_obs = rhoCosPhiP * Math.Cos(lst) * earthRadiusEqAU;
            double y_obs = rhoCosPhiP * Math.Sin(lst) * earthRadiusEqAU;
            double z_obs = rhoSinPhiP * earthRadiusEqAU;

            // Geocentric equatorial J2000 coordinates of the body in AU
            double x_geo = delta * Math.Cos(dec) * Math.Cos(ra);
            double y_geo = delta * Math.Cos(dec) * Math.Sin(ra);
            double z_geo = delta * Math.Sin(dec);

            // Topocentric equatorial J2000 coordinates of the body in AU
            double x_topo = x_geo - x_obs;
            double y_topo = y_geo - y_obs;
            double z_topo = z_geo - z_obs;

            double delta_topo = Math.Sqrt(x_topo * x_topo + y_topo * y_topo + z_topo * z_topo);

            raTopo = Math.Atan2(y_topo, x_topo);
            if (raTopo < 0) raTopo += 2 * Math.PI;

            decTopo = Math.Asin(z_topo / delta_topo);
            distTopo = new Distance(au: delta_topo);
        }

        /// <summary>
        /// Converts equatorial coordinates (Right Ascension and Declination) to horizontal coordinates (Altitude and Azimuth).
        /// </summary>
        /// <param name="ra">Right Ascension in radians.</param>
        /// <param name="dec">Declination in radians.</param>
        /// <param name="lat">Observer's latitude in radians.</param>
        /// <param name="lon">Observer's longitude in radians.</param>
        /// <param name="jd">Julian Date.</param>
        /// <returns>An array containing Altitude [0], Azimuth [1], and Hour Angle [2].</returns>
        static double[] RaDecToAltAz(double ra, double dec, double lat, double lon, double jd)
        {
            // Meeus 13.5 and 13.6, modified so West longitudes are negative and 0 is North
            double jd_ut = jd;
            double gmst = GreenwichMeanSiderealTime(jd_ut);
            double localSiderealTime = (gmst + lon) % (2 * Math.PI);

            double H = (localSiderealTime - ra);
            if (H < 0) { H += 2 * Math.PI; }
            if (H > Math.PI) { H -= 2 * Math.PI; }

            double az = Math.Atan2(Math.Sin(H), Math.Cos(H) * Math.Sin(lat) - Math.Tan(dec) * Math.Cos(lat));
            double a = Math.Asin(Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(H));
            az -= Math.PI;

            if (az < 0) { az += 2 * Math.PI; }
            return new double[] { a, az, H };
        }

        /// <summary>
        /// Calculates Greenwich Mean Sidereal Time (GMST) for a given Julian Date using the Capitaine et al. (IAU 2000) expressions.
        /// </summary>
        /// <param name="jd">Julian Date.</param>
        /// <returns>Greenwich Mean Sidereal Time in radians.</returns>
        public static double GreenwichMeanSiderealTime(double jd)
        {
            // "Expressions for IAU 2000 precession quantities" N. Capitaine1,P.T.Wallace2, and J. Chapront
            double t = (jd - 2451545.0) / 36525.0;

            double gmst = EarthRotationAngle(jd) + (0.014506 + 4612.156534 * t + 1.3915817 * t * t - 0.00000044 * t * t * t - 0.000029956 * t * t * t * t - 0.0000000368 * t * t * t * t * t) / 60.0 / 60.0 * Math.PI / 180.0;  // eq 42
            gmst %= 2 * Math.PI;
            if (gmst < 0) gmst += 2 * Math.PI;

            return gmst;
        }

        /// <summary>
        /// Calculates the Earth Rotation Angle (ERA) for a given Julian Date according to IERS Technical Note No. 32.
        /// </summary>
        /// <param name="jd">Julian Date.</param>
        /// <returns>Earth Rotation Angle in radians.</returns>
        public static double EarthRotationAngle(double jd)
        {
            // IERS Technical Note No. 32
            double t = jd - 2451545.0;
            double f = jd % 1.0;

            double theta = 2 * Math.PI * (f + 0.7790572732640 + 0.00273781191135448 * t); // eq 14
            theta %= 2 * Math.PI;
            if (theta < 0) theta += 2 * Math.PI;

            return theta;
        }

        /// <summary>
        /// Converts a local date and time of the observer to UTC.
        /// </summary>
        /// <param name="localTime">The local date and time.</param>
        /// <returns>The corresponding UTC date and time.</returns>
        public DateTime ConvertToUtc(DateTime localTime)
        {
            if (localTime.Kind == DateTimeKind.Utc)
            {
                return localTime;
            }
            if (localTime.Kind == DateTimeKind.Local)
            {
                return localTime.ToUniversalTime();
            }
            return TimeZoneInfo.ConvertTimeToUtc(localTime, TimeZone);
        }

        /// <summary>
        /// Converts a UTC date and time to the observer's local time.
        /// </summary>
        /// <param name="utcTime">The UTC date and time.</param>
        /// <returns>The corresponding local date and time.</returns>
        public DateTime ConvertToLocal(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Local && TimeZone.Equals(TimeZoneInfo.Local))
            {
                return utcTime;
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime.ToUniversalTime(), TimeZone);
        }
    }
}
