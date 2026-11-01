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

        public Vector(double x, double y) : this(x, y, 0) { }
        public Vector(double x) : this(x, 0, 0) { } 
        public Vector() : this(0, 0, 0) { }

        public Vector((double x1, double y1, double z1) start, (double x2, double y2, double z2) end)
        {
            X = end.x2 - start.x1;
            Y = end.y2 - start.y1;
            Z = end.z2 - start.z1;
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

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
