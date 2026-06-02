using EphemSharp.Units;
using System;
using EphemSharp.Bodies;

//ELP2000-82B
namespace EphemSharp.Data
{
    public static class Moon
    {
        private struct Delaunay
        {
            public double D;
            public double LP;
            public double L;
            public double F;
        }

        private struct MeanArgs
        {
            public double W1;
            public double W2;
            public double W3;
            public double T;
            public double OBP;
        }

        private static readonly double[][] H = new double[][]
        {
            new double[] { 785939.8782, 1732559343.3328, -6.87, 0.006604, -3169e-8 },
            new double[] { 300071.6518, 14643420.3304, -38.2639, -0.045047, 21301e-8 },
            new double[] { 450160.3265, -6967919.8851, 6.3593, 0.007625, -3586e-8 },
            new double[] { 361679.188, 129597742.3016, -0.0202, 9e-6, 15e-8 },
            new double[] { 370574.4136, 1161.2283, 0.5327, -138e-6, 0.0 }
        };

        private static readonly double[][] P_Coeffs = new double[][]
        {
            new double[] { 908103.25986, 538101628.68898 },
            new double[] { 655127.28305, 210664136.43355 },
            new double[] { 361679.22059, 129597742.2758 },
            new double[] { 1279559.78866, 68905077.59284 },
            new double[] { 123665.34212, 10925660.42861 },
            new double[] { 180278.89694, 4399609.65932 },
            new double[] { 1130598.01841, 1542481.19393 },
            new double[] { 1095655.19575, 786550.32074 }
        };

        private static readonly double rad_factor = Math.PI / 648000.0;
        private static readonly double t_const = 0.55604 * rad_factor / 1732559343.3328;
        private static readonly double P_const = 0.01789 * rad_factor;
        private static readonly double L_const = -0.08066 * rad_factor;
        private static readonly double n_const = -0.06424 * rad_factor / 1732559343.3328;
        private static readonly double E_const = -0.12879 * rad_factor;
        private static readonly double o_const = 0.02292188611773368;
        private static readonly double r_const = 0.074801329518;

        public static (Vector xyz, Distance r) XYZR(double jd)
        {
            // Geocentric rotated coordinates in kilometers
            Vector geoKM = CartesianJ2000Rotated(jd);

            // Convert to Astronomical Units (AU)
            double x_geo = geoKM.X / Distance.AU_KM;
            double y_geo = geoKM.Y / Distance.AU_KM;
            double z_geo = geoKM.Z / Distance.AU_KM;

            // Get Earth's heliocentric position
            var earthPos = Earth.XYZR(jd);

            // Heliocentric coordinates: Moon = Earth + geocentric Moon
            Vector moonHeliocentricXyz = earthPos.xyz + new Vector(x_geo, y_geo, z_geo);

            return (moonHeliocentricXyz, new Distance(au: moonHeliocentricXyz.Length()));
        }

        private static MeanArgs EvaluateMeanArgs(double i, int B)
        {
            double[] A = new double[5];
            for (int p = 0; p < 5; p++)
            {
                double e = 1.0;
                for (int t = 0; t < B; t++)
                {
                    A[p] += H[p][t] * e;
                    e *= i;
                }
            }
            return new MeanArgs
            {
                W1 = A[0],
                W2 = A[1],
                W3 = A[2],
                T = A[3],
                OBP = A[4]
            };
        }

        private static Delaunay L(MeanArgs args)
        {
            return new Delaunay
            {
                D = args.W1 - args.T + 648000.0,
                LP = args.T - args.OBP,
                L = args.W1 - args.W2,
                F = args.W1 - args.W3
            };
        }

        private static double[] GetG(double B)
        {
            double[] g = new double[8];
            for (int A = 0; A < 8; A++)
            {
                g[A] = P_Coeffs[A][0] + P_Coeffs[A][1] * B;
            }
            return g;
        }

        private static double ULonLat(Delaunay i, double[] A, double[] B, sbyte[] idx, double t_var)
        {
            double A_sum = 0;
            int numTerms = A.Length;
            for (int h = 0; h < numTerms; h++)
            {
                int idxBase = h * 4;
                int bBase = h * 6;
                double termVal = A[h] + (B[bBase] + o_const * B[bBase + 4]) * (n_const - r_const * t_var) +
                                 B[bBase + 1] * L_const + B[bBase + 2] * P_const + B[bBase + 3] * E_const;
                double angle = (idx[idxBase] * i.D + idx[idxBase + 1] * i.LP + idx[idxBase + 2] * i.L + idx[idxBase + 3] * i.F) * rad_factor;
                A_sum += termVal * Math.Sin(angle);
            }
            return A_sum;
        }

        private static double UDist(Delaunay i, double[] A, double[] B, sbyte[] idx)
        {
            double A_sum = 0;
            int numTerms = A.Length;
            for (int h = 0; h < numTerms; h++)
            {
                int idxBase = h * 4;
                int bBase = h * 6;
                double termVal = A[h] - 2.0 / 3.0 * A[h] * t_const +
                                 (B[bBase] + o_const * B[bBase + 4]) * (n_const - r_const * t_const) +
                                 B[bBase + 1] * L_const + B[bBase + 2] * P_const + B[bBase + 3] * E_const;
                double angle = (idx[idxBase] * i.D + idx[idxBase + 1] * i.LP + idx[idxBase + 2] * i.L + idx[idxBase + 3] * i.F) * rad_factor;
                A_sum += termVal * Math.Cos(angle);
            }
            return A_sum;
        }

        private static double C(double i_val, Delaunay B, double[] A, double[] phi, sbyte[] idx)
        {
            double h_val = 0;
            int numTerms = A.Length;
            for (int p = 0; p < numTerms; p++)
            {
                int idxBase = p * 11;
                double angle = (idx[idxBase] * i_val + idx[idxBase + 1] * B.D + idx[idxBase + 2] * B.LP + idx[idxBase + 3] * B.L + idx[idxBase + 4] * B.F) * rad_factor +
                               phi[p] * Math.PI / 180.0;
                h_val += A[p] * Math.Sin(angle);
            }
            return h_val;
        }

        private static double F_Pert(double[] i_arr, Delaunay B, double[] A, double[] phi, sbyte[] idx)
        {
            double h_val = 0;
            int numTerms = A.Length;
            for (int p = 0; p < numTerms; p++)
            {
                int idxBase = p * 11;
                double angle = (idx[idxBase] * i_arr[0] + idx[idxBase + 1] * i_arr[1] + idx[idxBase + 2] * i_arr[2] + idx[idxBase + 3] * i_arr[3] +
                                idx[idxBase + 4] * i_arr[4] + idx[idxBase + 5] * i_arr[5] + idx[idxBase + 6] * i_arr[6] + idx[idxBase + 7] * i_arr[7] +
                                idx[idxBase + 8] * B.D + idx[idxBase + 9] * B.L + idx[idxBase + 10] * B.F) * rad_factor +
                               phi[p] * Math.PI / 180.0;
                h_val += A[p] * Math.Sin(angle);
            }
            return h_val;
        }

        private static double S_Pert(double[] i_arr, Delaunay B, double[] A, double[] phi, sbyte[] idx)
        {
            double h_val = 0;
            int numTerms = A.Length;
            for (int p = 0; p < numTerms; p++)
            {
                int idxBase = p * 11;
                double angle = (idx[idxBase] * i_arr[0] + idx[idxBase + 1] * i_arr[1] + idx[idxBase + 2] * i_arr[2] + idx[idxBase + 3] * i_arr[3] +
                                idx[idxBase + 4] * i_arr[4] + idx[idxBase + 5] * i_arr[5] + idx[idxBase + 6] * i_arr[6] +
                                idx[idxBase + 7] * B.D + idx[idxBase + 8] * B.LP + idx[idxBase + 9] * B.L + idx[idxBase + 10] * B.F) * rad_factor +
                               phi[p] * Math.PI / 180.0;
                h_val += A[p] * Math.Sin(angle);
            }
            return h_val;
        }

        public static (double longitude, double latitude, double distance) SphericalJ2000(double jd)
        {
            double B_centuries = (jd - 2451545.0) / 36525.0;
            MeanArgs e_args = EvaluateMeanArgs(B_centuries, 5);
            Delaunay M_delaunay = L(e_args);
            Delaunay d_delaunay = L(EvaluateMeanArgs(B_centuries, 2));

            double b_val = H[0][0] + B_centuries * (H[0][1] + 5029.065);
            double[] g_arr = GetG(B_centuries);

            double y = ULonLat(M_delaunay, MoonData.ELP01_A, MoonData.ELP01_B, MoonData.ELP01_idx, t_const);
            double I = ULonLat(M_delaunay, MoonData.ELP02_A, MoonData.ELP02_B, MoonData.ELP02_idx, t_const);
            double O = UDist(M_delaunay, MoonData.ELP03_A, MoonData.ELP03_B, MoonData.ELP03_idx);

            y += C(b_val, d_delaunay, MoonData.ELP04_A, MoonData.ELP04_phi, MoonData.ELP04_idx);
            I += C(b_val, d_delaunay, MoonData.ELP05_A, MoonData.ELP05_phi, MoonData.ELP05_idx);
            O += C(b_val, d_delaunay, MoonData.ELP06_A, MoonData.ELP06_phi, MoonData.ELP06_idx);

            y += C(b_val, d_delaunay, MoonData.ELP07_A, MoonData.ELP07_phi, MoonData.ELP07_idx) * B_centuries;
            I += C(b_val, d_delaunay, MoonData.ELP08_A, MoonData.ELP08_phi, MoonData.ELP08_idx) * B_centuries;
            O += C(b_val, d_delaunay, MoonData.ELP09_A, MoonData.ELP09_phi, MoonData.ELP09_idx) * B_centuries;

            y += F_Pert(g_arr, d_delaunay, MoonData.ELP10_A, MoonData.ELP10_phi, MoonData.ELP10_idx);
            I += F_Pert(g_arr, d_delaunay, MoonData.ELP11_A, MoonData.ELP11_phi, MoonData.ELP11_idx);
            O += F_Pert(g_arr, d_delaunay, MoonData.ELP12_A, MoonData.ELP12_phi, MoonData.ELP12_idx);

            y += F_Pert(g_arr, d_delaunay, MoonData.ELP13_A, MoonData.ELP13_phi, MoonData.ELP13_idx) * B_centuries;
            I += F_Pert(g_arr, d_delaunay, MoonData.ELP14_A, MoonData.ELP14_phi, MoonData.ELP14_idx) * B_centuries;
            O += F_Pert(g_arr, d_delaunay, MoonData.ELP15_A, MoonData.ELP15_phi, MoonData.ELP15_idx) * B_centuries;

            y += S_Pert(g_arr, d_delaunay, MoonData.ELP16_A, MoonData.ELP16_phi, MoonData.ELP16_idx);
            I += S_Pert(g_arr, d_delaunay, MoonData.ELP17_A, MoonData.ELP17_phi, MoonData.ELP17_idx);
            O += S_Pert(g_arr, d_delaunay, MoonData.ELP18_A, MoonData.ELP18_phi, MoonData.ELP18_idx);

            y += S_Pert(g_arr, d_delaunay, MoonData.ELP19_A, MoonData.ELP19_phi, MoonData.ELP19_idx) * B_centuries;
            I += S_Pert(g_arr, d_delaunay, MoonData.ELP20_A, MoonData.ELP20_phi, MoonData.ELP20_idx) * B_centuries;
            O += S_Pert(g_arr, d_delaunay, MoonData.ELP21_A, MoonData.ELP21_phi, MoonData.ELP21_idx) * B_centuries;

            y += C(0, d_delaunay, MoonData.ELP22_A, MoonData.ELP22_phi, MoonData.ELP22_idx);
            I += C(0, d_delaunay, MoonData.ELP23_A, MoonData.ELP23_phi, MoonData.ELP23_idx);
            O += C(0, d_delaunay, MoonData.ELP24_A, MoonData.ELP24_phi, MoonData.ELP24_idx);

            y += C(0, d_delaunay, MoonData.ELP25_A, MoonData.ELP25_phi, MoonData.ELP25_idx) * B_centuries;
            I += C(0, d_delaunay, MoonData.ELP26_A, MoonData.ELP26_phi, MoonData.ELP26_idx) * B_centuries;
            O += C(0, d_delaunay, MoonData.ELP27_A, MoonData.ELP27_phi, MoonData.ELP27_idx) * B_centuries;

            y += C(0, d_delaunay, MoonData.ELP28_A, MoonData.ELP28_phi, MoonData.ELP28_idx);
            I += C(0, d_delaunay, MoonData.ELP29_A, MoonData.ELP29_phi, MoonData.ELP29_idx);
            O += C(0, d_delaunay, MoonData.ELP30_A, MoonData.ELP30_phi, MoonData.ELP30_idx);

            y += C(0, d_delaunay, MoonData.ELP31_A, MoonData.ELP31_phi, MoonData.ELP31_idx);
            I += C(0, d_delaunay, MoonData.ELP32_A, MoonData.ELP32_phi, MoonData.ELP32_idx);
            O += C(0, d_delaunay, MoonData.ELP33_A, MoonData.ELP33_phi, MoonData.ELP33_idx);

            y += C(0, d_delaunay, MoonData.ELP34_A, MoonData.ELP34_phi, MoonData.ELP34_idx) * B_centuries * B_centuries;
            I += C(0, d_delaunay, MoonData.ELP35_A, MoonData.ELP35_phi, MoonData.ELP35_idx) * B_centuries * B_centuries;
            O += C(0, d_delaunay, MoonData.ELP36_A, MoonData.ELP36_phi, MoonData.ELP36_idx) * B_centuries * B_centuries;

            y += e_args.W1;
            y *= rad_factor;
            I *= rad_factor;

            return (y, I, O);
        }

        public static Vector CartesianJ2000(double jd)
        {
            var spherical = SphericalJ2000(jd);
            double longitude = spherical.longitude;
            double latitude = spherical.latitude;
            double distance = spherical.distance;
            double x = distance * Math.Cos(longitude) * Math.Cos(latitude);
            double y = distance * Math.Sin(longitude) * Math.Cos(latitude);
            double z = distance * Math.Sin(latitude);
            return new Vector(x, y, z);
        }

        public static Vector CartesianJ2000Rotated(double jd)
        {
            Vector pos = CartesianJ2000(jd);
            double B = pos.X;
            double A = pos.Y;
            double h_val = pos.Z;

            double p_centuries = (jd - 2451545.0) / 36525.0;
            double e = 10180391e-12 * p_centuries + 4.7020439e-7 * p_centuries * p_centuries -
                       5.417367e-10 * p_centuries * p_centuries * p_centuries -
                       2507948e-18 * p_centuries * p_centuries * p_centuries * p_centuries +
                       463486e-20 * p_centuries * p_centuries * p_centuries * p_centuries * p_centuries;

            double t = -0.000113469002 * p_centuries + 1.2372674e-7 * p_centuries * p_centuries +
                       1.265417e-9 * p_centuries * p_centuries * p_centuries -
                       1371808e-18 * p_centuries * p_centuries * p_centuries * p_centuries -
                       320334e-20 * p_centuries * p_centuries * p_centuries * p_centuries * p_centuries;

            double P = Math.Sqrt(1.0 - e * e - t * t);

            double rx = (1.0 - 2.0 * e * e) * B + 2.0 * e * t * A + 2.0 * e * P * h_val;
            double ry = 2.0 * e * t * B + (1.0 - 2.0 * t * t) * A - 2.0 * t * P * h_val;
            double rz = -2.0 * e * P * B + 2.0 * t * P * A + (1.0 - 2.0 * e * e - 2.0 * t * t) * h_val;

            return new Vector(rx, ry, rz);
        }
    }
}
