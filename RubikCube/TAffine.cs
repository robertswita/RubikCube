/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej Inteligencji
***********************************************************/
using System;
using System.Collections.Generic;

namespace TGL
{
    [Serializable]
    public class TAffine : TMatrix
    {
        public static int N = 4;
        public TAffine() : base(4, 4) { LoadIdentity(); }
        public TAffine(TMatrix src) : base(4, 4) { Assign(src); }
        //public void Mult(TMatrix src)
        //{
        //    Assign(src * this);
        //}

        public static TAffine Scale(TVector scale)
        {
            var S = new TAffine();
            for (int i = 0; i < 3; i++)
                S[i, i] = scale[i];
            return S;
        }

        public static TAffine Shear(TVector h)
        {
            var H = new TAffine();
            H[0, 1] = h[0];
            H[0, 2] = h[1];
            H[1, 2] = h[2];
            return H;
        }

        public static TAffine RotateX(double alpha)
        {
            alpha *= Math.PI / 180;
            var cosA = (float)Math.Cos(alpha);
            var sinA = (float)Math.Sin(alpha);
            var R = new TAffine();
            R.Cols[1] = new TVector(0, cosA, sinA);
            R.Cols[2] = new TVector(0, -sinA, cosA);
            return R;
        }

        public static TAffine RotateY(double beta)
        {
            beta *= Math.PI / 180;
            var cosA = (float)Math.Cos(beta);
            var sinA = (float)Math.Sin(beta);
            var R = new TAffine();
            R.Cols[0] = new TVector(cosA, 0, -sinA);
            R.Cols[2] = new TVector(sinA, 0, cosA);
            return R;
        }

        public static TAffine RotateZ(double gamma)
        {
            gamma *= Math.PI / 180;
            var cosA = (float)Math.Cos(gamma);
            var sinA = (float)Math.Sin(gamma);
            var R = new TAffine();
            R.Cols[0] = new TVector(cosA, sinA, 0);
            R.Cols[1] = new TVector(-sinA, cosA, 0);
            return R;
        }
        public static TAffine Rotate(double angle, TVector v)
        {
            v.Norm = 1;
            var m = CreateRotation(v);
            return m * RotateX(angle) * m.Inv;
        }

        public static TAffine CreateRotation(TVector v)
        {
            var vX = new TVector();
            vX.Assign(v);
            var vY = new TVector(0, 1, 0);
            var vZ = new TVector(0, 0, 1);
            if (Math.Abs(vX.Y) < Math.Abs(vX.Z))
                vY = Cross(vX, vY);
            else
                vY = Cross(vX, vZ);
            vZ = Cross(vX, vY);
            var m = new TAffine();
            m.Cols[0] = vX;
            m.Cols[1] = vY;
            m.Cols[2] = vZ;
            return m;
        }

        public static TAffine Translate(TVector t)
        {
            var T = new TAffine();
            T.Cols[3] = t;
            return T;
        }

        public static TAffine operator *(TAffine affine, TMatrix M)
        {
            return new TAffine((TMatrix)affine * M);
        }

        public static TVector operator *(TAffine m, TVector v)
        {
            var p = new TVector(v[0], v[1], v[2], 1);
            p = (TMatrix)m * p;
            return new TVector(p[0] / p[3], p[1] / p[3], p[2] / p[3]);
        }

        public new TAffine Inv
        {
            get
            {
                return new TAffine(base.Inv);
            }
        }

        public TAffine Givens(int i, int j)
        {
            var a = this[j, j];
            var b = this[i, j];
            var r = (float)Math.Sqrt(a * a + b * b);
            var cos = a / r;
            var sin = -b / r;
            var Q = new TAffine();
            Q[j, j] = cos;
            Q[i, j] = sin;
            Q[j, i] = -sin;
            Q[i, i] = cos;
            return Q;
        }

        public List<TVector> GetEulerAngles()
        {
            //R = zeros(m * (m - 1) / 2, 1);
            var angles = new List<TVector>();
            var A = (TAffine)Clone();
            //var idx = 1;
            var Q = new TAffine();
            for (int i = 1; i < N; i++)
                for (int j = 0; j < i - 1; j++)
                {
                    var a = A[j, j];
                    var b = A[i, j];
                    //var phi = atan2(-b, a);
                    var r = (float)Math.Sqrt(a * a + b * b);
                    var cosA = a / r;
                    var sinA = -b / r;
                    angles.Add(new TVector(cosA, sinA));
                    //var c = cos(phi);
                    //var s = sin(phi);
                    //R(idx) = -phi;
                    //idx = idx + 1;
                    var Qij = new TAffine();
                    Qij[j, j] = cosA;
                    Qij[i, j] = sinA;
                    Qij[j, i] = -sinA;
                    Qij[i, i] = cosA;
                    A = Qij * A;
                    Q = Q * Qij.Transpose();
                }
            return angles;
        }


    };
}

