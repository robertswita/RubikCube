using System;

namespace RubikCube
{
    /// <summary>
    /// Static helper class for 4D matrix operations using 5x5 homogeneous coordinates
    /// </summary>
    public static class TMatrix4D
    {
        /// <summary>
        /// Multiply two 5x5 matrices: result = a * b
        /// </summary>
        public static void Multiply5x5(double[] a, double[] b, double[] result)
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    result[i * 5 + j] = 0;
                    for (int k = 0; k < 5; k++)
                    {
                        result[i * 5 + j] += a[i * 5 + k] * b[k * 5 + j];
                    }
                }
            }
        }

        /// <summary>
        /// Load identity matrix into 5x5 array
        /// </summary>
        public static void LoadIdentity5x5(double[] matrix)
        {
            for (int i = 0; i < 25; i++)
                matrix[i] = 0;
            matrix[0] = matrix[6] = matrix[12] = matrix[18] = matrix[24] = 1.0;
        }

        /// <summary>
        /// Create rotation matrix for XY plane (rotates X and Y, leaves Z and W fixed)
        /// </summary>
        public static void CreateRotationXY(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[0] = c;   result[1] = -s;  // Row 0: cos, -sin
            result[5] = s;   result[6] = c;   // Row 1: sin, cos
            // Rows 2, 3, 4 are identity (Z, W, homogeneous unchanged)
        }

        /// <summary>
        /// Create rotation matrix for XZ plane (rotates X and Z, leaves Y and W fixed)
        /// </summary>
        public static void CreateRotationXZ(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[0] = c;    result[2] = -s;   // Row 0: cos, 0, -sin
            result[10] = s;   result[12] = c;   // Row 2: sin, 0, cos
            // Rows 1, 3, 4 are identity (Y, W, homogeneous unchanged)
        }

        /// <summary>
        /// Create rotation matrix for XW plane (rotates X and W, leaves Y and Z fixed)
        /// </summary>
        public static void CreateRotationXW(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[0] = c;    result[3] = -s;   // Row 0: cos, 0, 0, -sin
            result[15] = s;   result[18] = c;   // Row 3: sin, 0, 0, cos
            // Rows 1, 2, 4 are identity (Y, Z, homogeneous unchanged)
        }

        /// <summary>
        /// Create rotation matrix for YZ plane (rotates Y and Z, leaves X and W fixed)
        /// </summary>
        public static void CreateRotationYZ(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[6] = c;    result[7] = -s;   // Row 1: 0, cos, -sin
            result[11] = s;   result[12] = c;   // Row 2: 0, sin, cos
            // Rows 0, 3, 4 are identity (X, W, homogeneous unchanged)
        }

        /// <summary>
        /// Create rotation matrix for YW plane (rotates Y and W, leaves X and Z fixed)
        /// </summary>
        public static void CreateRotationYW(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[6] = c;    result[8] = -s;   // Row 1: 0, cos, 0, -sin
            result[16] = s;   result[18] = c;   // Row 3: 0, sin, 0, cos
            // Rows 0, 2, 4 are identity (X, Z, homogeneous unchanged)
        }

        /// <summary>
        /// Create rotation matrix for ZW plane (rotates Z and W, leaves X and Y fixed)
        /// </summary>
        public static void CreateRotationZW(double angleDegrees, double[] result)
        {
            LoadIdentity5x5(result);
            double angleRad = angleDegrees * Math.PI / 180.0;
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);

            result[12] = c;   result[13] = -s;  // Row 2: 0, 0, cos, -sin
            result[17] = s;   result[18] = c;   // Row 3: 0, 0, sin, cos
            // Rows 0, 1, 4 are identity (X, Y, homogeneous unchanged)
        }

        /// <summary>
        /// Create translation matrix for 4D
        /// </summary>
        public static void CreateTranslation4D(double x, double y, double z, double w, double[] result)
        {
            LoadIdentity5x5(result);
            result[20] = x;   // Translation X in column 4, row 0
            result[21] = y;   // Translation Y in column 4, row 1
            result[22] = z;   // Translation Z in column 4, row 2
            result[23] = w;   // Translation W in column 4, row 3
        }

        /// <summary>
        /// Create scale matrix for 4D
        /// </summary>
        public static void CreateScale4D(double sx, double sy, double sz, double sw, double[] result)
        {
            LoadIdentity5x5(result);
            result[0] = sx;
            result[6] = sy;
            result[12] = sz;
            result[18] = sw;
        }

        /// <summary>
        /// Apply 5x5 matrix to 4D point (returns new point)
        /// </summary>
        public static TPoint4D TransformPoint(double[] matrix, TPoint4D point)
        {
            double x = matrix[0] * point.X + matrix[1] * point.Y + matrix[2] * point.Z + matrix[3] * point.W + matrix[20];
            double y = matrix[5] * point.X + matrix[6] * point.Y + matrix[7] * point.Z + matrix[8] * point.W + matrix[21];
            double z = matrix[10] * point.X + matrix[11] * point.Y + matrix[12] * point.Z + matrix[13] * point.W + matrix[22];
            double w = matrix[15] * point.X + matrix[16] * point.Y + matrix[17] * point.Z + matrix[18] * point.W + matrix[23];
            return new TPoint4D(x, y, z, w);
        }
    }

    /// <summary>
    /// 4D point structure
    /// </summary>
    public struct TPoint4D
    {
        public double X, Y, Z, W;

        public TPoint4D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
        }
    }
}
