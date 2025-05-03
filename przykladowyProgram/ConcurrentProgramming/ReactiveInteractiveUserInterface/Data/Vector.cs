//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//  by introducing yourself and telling us what you do with this community.
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data
{
    /// <summary>
    /// Two dimensions immutable vector
    /// </summary>
    internal record Vector : IVector
    {
        #region IVector

        public double X { get; init; }
        public double Y { get; init; }

        #endregion IVector

        public Vector(double XComponent, double YComponent)
        {
            X = XComponent;
            Y = YComponent;
        }

        public static Vector operator -(Vector a, Vector b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector operator +(Vector a, Vector b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector operator *(Vector v, double scalar) => new(v.X * scalar, v.Y * scalar);
        public static Vector operator *(double scalar, Vector v) => v * scalar;
        public static Vector operator /(Vector v, double scalar) => new(v.X / scalar, v.Y / scalar);

        public static double Dot(Vector a, Vector b) => a.X * b.X + a.Y * b.Y;

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vector Normalize()
        {
            double length = Length;
            return length == 0 ? new Vector(0, 0) : new Vector(X / length, Y / length);
        }

    }
}
