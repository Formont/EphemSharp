using EphemSharp.Units;
using EphemSharp.Data.vsop87;
using EphemSharp.Extensions;         
using System;
using System.Net.NetworkInformation;


namespace EphemSharp.Data.MeeusAlgorithms
{
    internal class Pluto
    {
        //Jean Meeus "Astronomical Algorithms", chapter 37
        //J, S, P, Longitude A, Longitude B, Latitude A, Latitude B, Radius vector A, Radius vector B
        private static readonly int[,] table = new int[43, 9]
        {
            { 0, 0, 1, -19799805, 19850055, -5452852, -14974862, 66865439, 68951812 },
            { 0, 0, 2, 897144, -4954829, 3527812, 1672790, -11827535, -332538 },
            { 0, 0, 3, 611149, 1211027, -1050748, 327647, 1593179, -1438890 },
            { 0, 0, 4, -341243, -189585, -178690, -292153, -18444, 483220 },
            { 0, 0, 5, 129287, -34992, 18650, 100340, -65977, -85431 },
            { 0, 0, 6, -38164, 30893, -30697, -25823, 31174, -6032 },
            { 0, 1, -1, 20442, -9987, 4878, 11248, -5794, 22161 },
            { 0, 1, 0, -4063, -5071, 226, -64, 4601, 4032 },
            { 0, 1, 1, -6016, -3336, 2030, -836, -1729, 234 },
            { 0, 1, 2, -3956, 3039, 69, -604, -415, 702 },
            { 0, 1, 3, -667, 3572, -247, -567, 239, 723 },
            { 0, 2, -2, 1276, 501, -57, 1, 67, -67 },
            { 0, 2, -1, 1152, -917, -122, 175, 1034, -451 },
            { 0, 2, 0, 630, -1277, -49, -164, -129, 504 },
            { 1, -1, 0, 2571, -459, -197, 199, 480, -231 },
            { 1, -1, 1, 899, -1449, -25, 217, 2, -441 },
            { 1, 0, -3, -1016, 1043, 589, -248, -3359, 265 },
            { 1, 0, -2, -2343, -1012, -269, 711, 7856, -7832 },
            { 1, 0, -1, 7042, 788, 185, 193, 36, 45763 },
            { 1, 0, 0, 1199, -338, 315, 807, 8663, 8547 },
            { 1, 0, 1, 418, -67, -130, -43, -809, -769 },
            { 1, 0, 2, 120, -274, 5, 3, 263, -144 },
            { 1, 0, 3, -60, -159, 2, 17, -126, 32 },
            { 1, 0, 4, -82, -29, 2, 5, -35, -16 },
            { 1, 1, -3, -36, -29, 2, 3, -19, -4 },
            { 1, 1, -2, -40, 7, 3, 1, -15, 8 },
            { 1, 1, -1, -14, 22, 2, -1, -4, 12 },
            { 1, 1, 0, 4, 13, 1, -1, 5, 6 },
            { 1, 1, 1, 5, 2, 0, -1, 3, 1 },
            { 1, 1, 3, -1, 0, 0, 0, 6, -2 },
            { 2, -1, -6, -2, 0, 0, -2, 2, 2 },
            { 2, 0, -5, -4, 5, 3, 2, -2, -2 },
            { 2, 0, -4, 4, -7, -7, 0, 14, 13 },
            { 2, 0, -3, 14, 24, -10, -8, -63, 13 },
            { 2, 0, -2, -49, -34, -3, 20, 136, -236 },
            { 2, 0, -1, 163, -48, 6, 5, 273, 1065 },
            { 2, 0, 0, 9, -24, 14, 17, 251, 149 },
            { 2, 0, 1, -4, 1, -2, 0, -25, -9 },
            { 2, 0, 2, -3, 1, 0, 0, 9, -2 },
            { 2, 0, 3, -1, 3, 0, 0, -8, 7 },
            { 3, 0, -2, -3, -1, 0, 1, 2, -10 },
            { 3, 0, -1, 5, -3, 0, 0, 19, 35 },
            { 3, 0, 0, 0, 0, 1, 0, 10, 3 }
        };

        internal static (Vector xyz, Distance r) XYZR(AstroTime time)
        {
            double T = time.JulianCenturiesTT;

            double J = 34.35 + 3034.9057 * T;
            double S = 50.08 + 1222.1138 * T;
            double P = 238.96 + 144.9600 * T;

            double l = 238.9508 + 144.9600 * T;
            double b = -3.9082239;
            double r = 40.7241346;

            for (int a = 0; a < table.GetLength(0); a++)
            {
                int i = table[a, 0];
                int j = table[a, 1];
                int k = table[a, 2];
                double alpha = i * J + j * S + k * P;
                double sinArg = Math.Sin(alpha.ToRadians());
                double cosArg = Math.Cos(alpha.ToRadians());
                l += (table[a, 3] * sinArg + table[a, 4] * cosArg) / 1_000_000.0;
                b += (table[a, 5] * sinArg + table[a, 6] * cosArg) / 1_000_000.0;
                r += (table[a, 7] * sinArg + table[a, 8] * cosArg) / 10_000_000.0;
            }

            double cose = 0.917482062;
            double sine = 0.397777156;

            //Heocentric coords
            double X = r * Math.Cos(l.ToRadians()) * Math.Cos(b.ToRadians());
            double Y = r * (Math.Sin(l.ToRadians()) * Math.Cos(b.ToRadians()) * cose - Math.Sin(b.ToRadians()) * sine);
            double Z = r * (Math.Sin(l.ToRadians()) * Math.Cos(b.ToRadians()) * sine + Math.Sin(b.ToRadians()) * cose);

            double Xecl = X;
            double Yecl = Y * cose + Z * sine;
            double Zecl = -Y * sine + Z * cose;

            Vector Phelio = new Vector(Xecl, Yecl, Zecl);
            return (Phelio, new Distance(au: Phelio.Length()));
        }

        internal static (Vector xyz, Distance r) XYZRWithLightTime(AstroTime time)
        {
            var earth = Earth.XYZR(time);

            // Первоначальная оценка положения Pluto
            var pluto = XYZR(time);

            double delta = (pluto.xyz - earth.xyz).Length();

            // Свет идёт 0.0057755183 суток на 1 AU
            double tau = delta * 0.0057755183;

            // Pluto нужно вычислить для более раннего момента
            var plutoRetarded = XYZR(new AstroTime(time.JD - tau));

            var eclVector = J2000ToEclipticOfDate(plutoRetarded.xyz, time);
            return (eclVector, new Distance(au: eclVector.Length()));
        }

        private static Vector J2000ToEclipticOfDate(Vector v, AstroTime time)
        {
            double T = time.JulianCenturiesTT;

            double zetaA =
                (2306.2181 * T
                + 0.30188 * T * T
                + 0.017998 * T * T * T) / 3600.0;

            double zA =
                (2306.2181 * T
                + 1.09468 * T * T
                + 0.018203 * T * T * T) / 3600.0;

            double thetaA =
                (2004.3109 * T
                - 0.42665 * T * T
                - 0.041833 * T * T * T) / 3600.0;

            double zeta = zetaA.ToRadians();
            double z = zA.ToRadians();
            double theta = thetaA.ToRadians();

            // Precession matrix J2000 -> equinox of date
            double A11 = Math.Cos(zeta) * Math.Cos(theta) * Math.Cos(z)
                      - Math.Sin(zeta) * Math.Sin(z);

            double A12 = -Math.Sin(zeta) * Math.Cos(theta) * Math.Cos(z)
                      - Math.Cos(zeta) * Math.Sin(z);

            double A13 = -Math.Sin(theta) * Math.Cos(z);

            double A21 = Math.Cos(zeta) * Math.Cos(theta) * Math.Sin(z)
                      + Math.Sin(zeta) * Math.Cos(z);

            double A22 = -Math.Sin(zeta) * Math.Cos(theta) * Math.Sin(z)
                      + Math.Cos(zeta) * Math.Cos(z);

            double A23 = -Math.Sin(theta) * Math.Sin(z);

            double A31 = Math.Cos(zeta) * Math.Sin(theta);

            double A32 = -Math.Sin(zeta) * Math.Sin(theta);

            double A33 = Math.Cos(theta);

            double x =
                A11 * v.X +
                A12 * v.Y +
                A13 * v.Z;

            double y =
                A21 * v.X +
                A22 * v.Y +
                A23 * v.Z;

            double zEq =
                A31 * v.X +
                A32 * v.Y +
                A33 * v.Z;

            // Mean obliquity of date
            double epsilon =
                (23.4392911111111
                - 0.0130041666667 * T
                - 1.64e-7 * T * T
                + 5.04e-7 * T * T * T).ToRadians();

            // Equatorial of date -> ecliptic of date
            double xecl = x;

            double yecl =
                y * Math.Cos(epsilon)
                + zEq * Math.Sin(epsilon);

            double zecl =
                -y * Math.Sin(epsilon)
                + zEq * Math.Cos(epsilon);

            return new Vector(xecl, yecl, zecl);
        }

    }
}
