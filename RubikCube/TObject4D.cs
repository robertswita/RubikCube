using System;
using System.Collections.Generic;

namespace RubikCube
{
    /// <summary>
    /// Base class for 4D objects with transformation matrix support
    /// </summary>
    public class TObject4D
    {
        /// <summary>
        /// 5x5 transformation matrix (homogeneous 4D coordinates)
        /// Layout: [m00, m01, m02, m03, tx,
        ///          m10, m11, m12, m13, ty,
        ///          m20, m21, m22, m23, tz,
        ///          m30, m31, m32, m33, tw,
        ///          0,   0,   0,   0,   1]
        /// </summary>
        public double[] Transform = new double[25];

        /// <summary>
        /// Parent object in scene hierarchy
        /// </summary>
        public TObject4D Parent;

        /// <summary>
        /// Child objects
        /// </summary>
        public List<TObject4D> Children = new List<TObject4D>();

        public TObject4D()
        {
            TMatrix4D.LoadIdentity5x5(Transform);
        }

        /// <summary>
        /// Get origin (position) from transformation matrix
        /// </summary>
        public TPoint4D Origin
        {
            get
            {
                return new TPoint4D(Transform[20], Transform[21], Transform[22], Transform[23]);
            }
        }

        /// <summary>
        /// Load identity transformation
        /// </summary>
        public void LoadIdentity()
        {
            TMatrix4D.LoadIdentity5x5(Transform);
        }

        /// <summary>
        /// Multiply this transformation by another 5x5 matrix
        /// </summary>
        public void MultMatrix(double[] matrix)
        {
            var result = new double[25];
            TMatrix4D.Multiply5x5(Transform, matrix, result);
            Array.Copy(result, Transform, 25);
        }

        /// <summary>
        /// Rotate in XY plane (rotates X and Y coordinates, Z and W unchanged)
        /// </summary>
        public void RotateXY(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXY(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Rotate in XZ plane (rotates X and Z coordinates, Y and W unchanged)
        /// </summary>
        public void RotateXZ(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXZ(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Rotate in XW plane (rotates X and W coordinates, Y and Z unchanged)
        /// </summary>
        public void RotateXW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXW(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Rotate in YZ plane (rotates Y and Z coordinates, X and W unchanged)
        /// </summary>
        public void RotateYZ(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationYZ(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Rotate in YW plane (rotates Y and W coordinates, X and Z unchanged)
        /// </summary>
        public void RotateYW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationYW(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Rotate in ZW plane (rotates Z and W coordinates, X and Y unchanged)
        /// </summary>
        public void RotateZW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationZW(angleDegrees, rotMatrix);
            MultMatrix(rotMatrix);
        }

        /// <summary>
        /// Translate in 4D space
        /// </summary>
        public void Translate4D(double x, double y, double z, double w)
        {
            var transMatrix = new double[25];
            TMatrix4D.CreateTranslation4D(x, y, z, w, transMatrix);
            MultMatrix(transMatrix);
        }

        /// <summary>
        /// Scale in 4D space
        /// </summary>
        public void Scale4D(double sx, double sy, double sz, double sw)
        {
            var scaleMatrix = new double[25];
            TMatrix4D.CreateScale4D(sx, sy, sz, sw, scaleMatrix);
            MultMatrix(scaleMatrix);
        }

        /// <summary>
        /// Helper method to rotate based on plane index
        /// </summary>
        public void RotateByPlane(int plane, double angleDegrees)
        {
            switch (plane)
            {
                case 0: RotateXY(angleDegrees); break;
                case 1: RotateXZ(angleDegrees); break;
                case 2: RotateXW(angleDegrees); break;
                case 3: RotateYZ(angleDegrees); break;
                case 4: RotateYW(angleDegrees); break;
                case 5: RotateZW(angleDegrees); break;
                default: throw new ArgumentException($"Invalid plane index: {plane}. Must be 0-5.");
            }
        }
    }
}
