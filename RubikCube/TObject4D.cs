using System;
using System.Collections.Generic;
using TGL;

namespace RubikCube
{
    /// <summary>
    /// Base class for 4D objects - extends TObject3D with 4D transformation support
    /// Inherits 3D rendering capabilities (Vertices, Faces, 4×4 Transform) from TObject3D
    /// Adds 4D transformation matrix (5×5) for true 4D operations
    /// </summary>
    public class TObject4D : TObject3D
    {
        /// <summary>
        /// 5x5 transformation matrix for 4D operations (homogeneous 4D coordinates)
        /// Layout: [m00, m01, m02, m03, tx,
        ///          m10, m11, m12, m13, ty,
        ///          m20, m21, m22, m23, tz,
        ///          m30, m31, m32, m33, tw,
        ///          0,   0,   0,   0,   1]
        /// Note: The 3D Transform (4×4) from base class is used for rendering
        ///       The Transform4D (5×5) is used for 4D rotations and position tracking
        /// </summary>
        public double[] Transform4D = new double[25];

        public TObject4D()
        {
            TMatrix4D.LoadIdentity5x5(Transform4D);
        }

        /// <summary>
        /// Get 4D origin (position) from 4D transformation matrix
        /// Returns: TPoint4D with (x, y, z, w) coordinates
        /// </summary>
        public new TPoint4D Origin4D
        {
            get
            {
                return new TPoint4D(Transform4D[4], Transform4D[9], Transform4D[14], Transform4D[19]);
            }
        }

        /// <summary>
        /// Load identity transformation for 4D matrix
        /// </summary>
        public void LoadIdentity4D()
        {
            TMatrix4D.LoadIdentity5x5(Transform4D);
        }

        /// <summary>
        /// Multiply this transformation by another 5x5 matrix
        /// </summary>
        public void MultMatrix4D(double[] matrix)
        {
            var result = new double[25];
            TMatrix4D.Multiply5x5(Transform4D, matrix, result);
            Array.Copy(result, Transform4D, 25);
        }

        /// <summary>
        /// Rotate in XY plane (rotates X and Y coordinates, Z and W unchanged)
        /// </summary>
        public void RotateXY(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXY(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Rotate in XZ plane (rotates X and Z coordinates, Y and W unchanged)
        /// </summary>
        public void RotateXZ(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXZ(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Rotate in XW plane (rotates X and W coordinates, Y and Z unchanged)
        /// </summary>
        public void RotateXW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationXW(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Rotate in YZ plane (rotates Y and Z coordinates, X and W unchanged)
        /// </summary>
        public void RotateYZ(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationYZ(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Rotate in YW plane (rotates Y and W coordinates, X and Z unchanged)
        /// </summary>
        public void RotateYW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationYW(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Rotate in ZW plane (rotates Z and W coordinates, X and Y unchanged)
        /// </summary>
        public void RotateZW(double angleDegrees)
        {
            var rotMatrix = new double[25];
            TMatrix4D.CreateRotationZW(angleDegrees, rotMatrix);
            MultMatrix4D(rotMatrix);
        }

        /// <summary>
        /// Translate in 4D space
        /// </summary>
        public void Translate4D(double x, double y, double z, double w)
        {
            var transMatrix = new double[25];
            TMatrix4D.CreateTranslation4D(x, y, z, w, transMatrix);
            MultMatrix4D(transMatrix);
        }

        /// <summary>
        /// Scale in 4D space
        /// </summary>
        public void Scale4D(double sx, double sy, double sz, double sw)
        {
            var scaleMatrix = new double[25];
            TMatrix4D.CreateScale4D(sx, sy, sz, sw, scaleMatrix);
            MultMatrix4D(scaleMatrix);
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
