using System;
using System.Collections.Generic;
using System.Drawing;

using OpenTK;

namespace MD
{
    /// <summary>
    /// A two-dimensional floating point vector.
    /// </summary>
    public struct Vector
    {
        public Vector(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>
        /// Gets the square of the length of this vector. This function is quicker to compute than the actual length
        /// because it avoids a square root, which may be costly.
        /// </summary>
        public double SquareLength
        {
            get
            {
                return this.X * this.X + this.Y * this.Y;
            }
        }

        /// <summary>
        /// Gets the length of the vector.
        /// </summary>
        public double Length
        {
            get
            {
                return Math.Sqrt(this.SquareLength);
            }
        }

        /// <summary>
        /// Creates a unit vector for the specified angle.
        /// </summary>
        public static Vector Unit(double Angle)
        {
            return new Vector(Math.Sin(Angle), Math.Cos(Angle));
        }

        /// <summary>
        /// Gets the angle of this vector.
        /// </summary>
        public double Angle
        {
            get
            {
                return Math.Atan2(this.Y, this.X);
            }
        }

        /// <summary>
        /// Gets if the given angle is an interior angle of the two specified angles. All angles must be
        /// normalized.
        /// </summary>
        public static bool AngleBetween(double Angle, double LowAngle, double HighAngle)
        {
            if (LowAngle < HighAngle && Angle >= LowAngle && Angle <= HighAngle)
            {
                return true;
            }
            if (LowAngle > HighAngle && (Angle >= LowAngle || Angle <= HighAngle))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the dot product of two vectors.
        /// </summary>
        public static double Dot(Vector A, Vector B)
        {
            return A.X * B.X + A.Y * B.Y;
        }

        public static implicit operator Vector2(Vector Vector)
        {
            return new Vector2((float)Vector.X, (float)Vector.Y);
        }

        public static implicit operator PointF(Vector Vector)
        {
            return new PointF((float)Vector.X, (float)Vector.Y);
        }

        public static Vector operator -(Vector A, Vector B)
        {
            return new Vector(A.X - B.X, A.Y - B.Y);
        }

        public static Vector operator -(Vector A)
        {
            return new Vector(-A.X, -A.Y);
        }

        public static Vector operator +(Vector A, Vector B)
        {
            return new Vector(A.X + B.X, A.Y + B.Y);
        }

        public static Vector operator *(Vector A, double B)
        {
            return new Vector(A.X * B, A.Y * B);
        }

        public double X;
        public double Y;
    }
}