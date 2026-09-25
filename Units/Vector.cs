using System;

namespace EphemSharp.Units
{
    public class Vector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Vector(double x, double y, double z) 
        { 
            X = x;  
            Y = y;
            Z = z;
        }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        // Вычитание векторов
        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector operator *(Vector a, double scalar)
        {
            return new Vector(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        public static Vector operator *(double scalar, Vector a)
        {
            return a * scalar;
        }

        public static double operator *(Vector a, Vector b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static double AngleBetween(Vector a, Vector b)
        {
            double dotProduct = a * b;
            double lengthsProduct = a.Length() * b.Length();
            return Math.Acos(dotProduct / lengthsProduct);
        }
    }
}
