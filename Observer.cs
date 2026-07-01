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
            Angle angularSizeTopo;
            GetTopocentricCoords(body, jd, out raTopo, out decTopo, out distTopo, out angularSizeTopo);

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
            return new ObservedObject(alt, az, h, ra, dec, distTopo, angularSizeTopo);
        }

        private void GetTopocentricCoords(CelestialBody body, double jd, out double raTopo, out double decTopo, out Distance distTopo, out Angle angularSizeTopo)
        {
            Distance geoDistance = null;
            Angle? geoAngularSize = null;
            if (body is Planet planet)
            {
                geoDistance = planet.EarthDistance;
                geoAngularSize = planet.AngularSize;
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
                angularSizeTopo = geoAngularSize ?? new Angle(AngleType.Degrees, 0);
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

            // Calculate Precession Matrix from J2000 to epoch of date (Meeus Chapter 21)
            double T = (jd - 2451545.0) / 36525.0;
            double T2 = T * T;
            double T3 = T2 * T;

            // In arcseconds
            double zeta_a = 2306.2181 * T + 0.30188 * T2 + 0.017998 * T3;
            double z_a = 2306.2181 * T + 1.09468 * T2 + 0.018203 * T3;
            double theta_a = 2004.3109 * T - 0.42665 * T2 - 0.041833 * T3;

            // Convert to radians
            double zeta = zeta_a / 3600.0 * Math.PI / 180.0;
            double z_rad = z_a / 3600.0 * Math.PI / 180.0;
            double theta = theta_a / 3600.0 * Math.PI / 180.0;

            double cz = Math.Cos(z_rad);
            double sz = Math.Sin(z_rad);
            double ctheta = Math.Cos(theta);
            double stheta = Math.Sin(theta);
            double czeta = Math.Cos(zeta);
            double szeta = Math.Sin(zeta);

            double p11 = cz * ctheta * czeta - sz * szeta;
            double p12 = -cz * ctheta * szeta - sz * czeta;
            double p13 = -cz * stheta;

            double p21 = sz * ctheta * czeta + cz * szeta;
            double p22 = -sz * ctheta * szeta + cz * czeta;
            double p23 = -sz * stheta;

            double p31 = stheta * czeta;
            double p32 = -stheta * szeta;
            double p33 = ctheta;

            // Geocentric equatorial coordinates of the body for the epoch of date in AU
            double x_date = p11 * x_geo + p12 * y_geo + p13 * z_geo;
            double y_date = p21 * x_geo + p22 * y_geo + p23 * z_geo;
            double z_date = p31 * x_geo + p32 * y_geo + p33 * z_geo;

            // Topocentric equatorial coordinates of the body for the epoch of date in AU
            double x_topo = x_date - x_obs;
            double y_topo = y_date - y_obs;
            double z_topo = z_date - z_obs;

            double delta_topo = Math.Sqrt(x_topo * x_topo + y_topo * y_topo + z_topo * z_topo);

            raTopo = Math.Atan2(y_topo, x_topo);
            if (raTopo < 0) raTopo += 2 * Math.PI;

            decTopo = Math.Asin(z_topo / delta_topo);
            distTopo = new Distance(au: delta_topo);

            if (geoAngularSize != null && geoAngularSize.Value.Radians > 0)
            {
                double radiusKm = Math.Sin(geoAngularSize.Value.Radians / 2.0) * geoDistance.KM;
                angularSizeTopo = new Angle(AngleType.Degrees, Math.Asin(radiusKm / distTopo.KM) * 2.0);
            }
            else
            {
                angularSizeTopo = new Angle(AngleType.Degrees, 0);
            }
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
