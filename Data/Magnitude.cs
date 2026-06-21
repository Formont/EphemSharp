using EphemSharp.Enums;
using System;
using static System.Math;

namespace EphemSharp.Data
{
    public static class MagnitudeCounter
    {
        public static double GetMagnitude(Planets planet, double r, double delta, double phaseAngle)
        {
            switch (planet)
            {
                case Planets.Mercury: return MercuryMagnitude(r, delta, phaseAngle);
                case Planets.Venus: return VenusMagnitude(r, delta, phaseAngle);
                case Planets.Earth: return EarthMagnitude(r, delta, phaseAngle);
                case Planets.Mars: return MarsMagnitude(r, delta, phaseAngle);
                case Planets.Jupiter: return JupiterMagnitude(r, delta, phaseAngle);
                case Planets.Saturn: return SaturnMagnitude(r, delta, phaseAngle);
                case Planets.Uranus: return UranusMagnitude(r, delta, phaseAngle);
                case Planets.Neptune: return NeptuneMagnitude(r, delta, phaseAngle);   
                case Planets.Moon: return MoonMagnitude(r, delta, phaseAngle);
                case Planets.Sun: return SunMagnitude(delta);
                default: return 99.99;
            }
        }

        private static double MercuryMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            double ph = phaseAngle;

            double phAngFactor =
                6.3280e-02 * ph
                - 1.6336e-03 * Pow(ph, 2)
                + 3.3644e-05 * Pow(ph, 3)
                - 3.4265e-07 * Pow(ph, 4)
                + 1.6893e-09 * Pow(ph, 5)
                - 3.0334e-12 * Pow(ph, 6);

            return -0.613 + distanceMagFactor + phAngFactor;
        }

        private static double VenusMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            double ph = phaseAngle;

            bool condition = ph < 163.7;

            double a0 = condition ? 0.0 : 236.05828 + 4.384;
            double a1 = condition ? -1.044E-03 : -2.81914E+00;
            double a2 = condition ? 3.687E-04 : 8.39034E-03;
            double a3 = condition ? -2.814E-06 : 0.0;
            double a4 = condition ? 8.938E-09 : 0.0;

            double phAngFactor = (((((a4 * ph + a3) * ph + a2) * ph) + a1) * ph) + a0;

            return -4.384 + distanceMagFactor + phAngFactor;
        }

        private static double EarthMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            double phAngFactor = -1.060e-03 * phaseAngle + 2.054e-04 * Pow(phaseAngle, 2);
            return -3.99 + distanceMagFactor + phAngFactor;
        }

        private static double MarsMagnitude(double r, double delta, double phaseAngle)
        {
            double rMagFactor = 2.5 * Log10(r * r);
            double deltaMagFactor = 2.5 * Log10(delta * delta);
            double distanceMagFactor = rMagFactor + deltaMagFactor;

            double ph = phaseAngle;
            bool condition = ph <= 50.0;

            double a = condition ? 2.267E-02 : -0.02573;
            double b = condition ? -1.302E-04 : 0.0003445;
            double phAngFactor = a * ph + b * ph * ph;

            double apMag = condition ? -1.601 : -0.367;
            return apMag + distanceMagFactor + phAngFactor;
        }

        private static double JupiterMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            double ph = phaseAngle;
            double phAngPi = ph / 180.0;

            double phAngFactor = ph <= 12.0
                ? (6.16E-04 * ph - 3.7E-04) * ph
                : -2.5 * Log10(((((-1.876 * phAngPi + 2.809) * phAngPi - 0.062) * phAngPi - 0.363) * phAngPi - 1.507) * phAngPi + 1.0);

            double baseMag = ph <= 12.0 ? -9.395 : -9.428;

            return baseMag + distanceMagFactor + phAngFactor;
        }

        private static double SaturnMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            // Simple Saturn magnitude formula (without complex ring system modeling)
            return -8.88 + distanceMagFactor + 0.044 * phaseAngle;
        }

        private static double UranusMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            return -7.19 + distanceMagFactor + 0.0028 * phaseAngle;
        }

        private static double NeptuneMagnitude(double r, double delta, double phaseAngle)
        {
            double distanceMagFactor = 5 * Log10(r * delta);
            return -6.87 + distanceMagFactor;
        }

        private static double MoonMagnitude(double r, double delta, double phaseAngle)
        {
            double psi = phaseAngle * PI / 180.0;
            double baseMag = -12.73 + 1.49 * Abs(psi) + 0.043 * Pow(psi, 4);
            double distCorr = 5 * Log10(delta / 0.00257);
            return baseMag + distCorr;
        }

        private static double SunMagnitude(double delta)
        {
            return -26.74 + 5 * Log10(delta);
        }
    }
}
