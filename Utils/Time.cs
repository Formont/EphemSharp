using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemSharp.Utils
{
    public static class Time
    {
        public static double ToJulianDate(DateTime dateTime)
        {
            int year = dateTime.Year;
            int month = dateTime.Month;
            double day = dateTime.Day +
                         dateTime.Hour / 24.0 +
                         dateTime.Minute / 1440.0 +
                         dateTime.Second / 86400.0 +
                         dateTime.Millisecond / 86400000.0;

            if (month <= 2)
            {
                year--;
                month += 12;
            }

            int A = year / 100;
            int B = 2 - A + (A / 4);

            double jd = Math.Floor(365.25 * (year + 4716))
                      + Math.Floor(30.6001 * (month + 1))
                      + day + B - 1524.5;

            return jd;
        }

    }
}
