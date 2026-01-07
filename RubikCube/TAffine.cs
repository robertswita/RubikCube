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
        public TAffine() : base(N + 1, N + 1) { LoadIdentity(); }
        public TAffine(TMatrix src) : base(N + 1, N + 1) { Assign(src); }
        public static int[][] Planes = new int[N * (N - 1) / 2][];
        static TAffine()
        {
            var idx = 0;
            for (int i = 0; i < N - 1; i++)
                for (int j = i + 1; j < N; j++)
                {
                    Planes[idx] = new int[] { i, j };
                    idx++;
                }
        }

        public static TAffine CreateScale(TVector scale)
        {
            var S = new TAffine();
            for (int i = 0; i < N; i++)
                S[i, i] = scale[i];
            return S;
        }

        public static TAffine CreateShear(TVector h)
        {
            var H = new TAffine();
            for (int i = 0; i < Planes.Length; i++)
                H[Planes[i][0], Planes[i][1]] = h[i];
            return H;
        }

        public static TAffine CreateRotation(int axis1, int axis2, double angle)
        {
            angle *= Math.PI / 180;
            var cosA = (float)Math.Cos(angle);
            var sinA = (float)Math.Sin(angle);
            var R = new TAffine();
            R[axis1, axis1] = cosA;
            R[axis2, axis2] = cosA;
            R[axis1, axis2] = -sinA;
            R[axis2, axis1] = sinA;
            return R;
        }

        public static TAffine CreateRotation(int axis, double angle)
        {
            return CreateRotation(Planes[axis][0], Planes[axis][1], angle);
        }

        public static TAffine CreateTranslation(TVector t)
        {
            var T = new TAffine();
            for (int i = 0; i < N; i++)
                T[i, N] = t[i];
            return T;
        }

        public override TVector Clone()
        {
            TAffine result = new TAffine();
            result.Assign(this);
            return result;
        }

        public static TAffine operator *(TAffine affine, TMatrix M)
        {
            return new TAffine((TMatrix)affine * M);
        }

        public static TVector operator *(TAffine m, TVector v)
        {
            var p = new TVector(N + 1);
            Array.Copy(v.Data, p.Data, N);
            p[N] = 1;
            p = (TMatrix)m * p;
            p = p / p[N];
            var v_ = new TVector(N);
            Array.Copy(p.Data, v_.Data, N);
            return v_;
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

        public List<TVector> GetEulerAngles2()
        {
            //R = zeros(m * (m - 1) / 2, 1);
            var angles = new List<TVector>();
            var A = (TAffine)Clone();
            //var A = new TMatrix(N, N);
            //for (int x = 0; x < N; x++)
            //{
            //    var col = new TVector(N);
            //    Array.Copy(Cols[x].Data, col.Data, N);
            //    A.Cols[x] = col;
            //}
            //var idx = 1;
            //var Q = new TAffine();
            for (int i = 0; i < N - 1; i++)
                for (int j = i + 1; j < N; j++)
                {
                    var a = A[i, i];
                    var b = A[j, i];
                    //var phi = Math.Atan2(-b, a);

                    var r = (float)Math.Sqrt(a * a + b * b);
                    var cosA = a / r;
                    var sinA = -b / r;
                    //var cosA = (float)Math.Cos(phi);
                    //var sinA = (float)Math.Sin(phi);
                    angles.Add(new TVector(cosA, sinA));
                    //R(idx) = -phi;
                    //idx = idx + 1;
                    var Qij = new TAffine();
                    Qij[j, j] = cosA;
                    Qij[i, j] = -sinA;
                    Qij[j, i] = sinA;
                    Qij[i, i] = cosA;
                    A = Qij * A;
                    //Q = Q * Qij.Transpose();
                }
            return angles;
        }

        public List<TVector> GetEulerAngles()
        {
            var angles = new List<TVector>();
            var A = (TAffine)Clone();
            //var Q = new TAffine();
            for (int n = 0; n < N - 1; n++)
                for (int m = n + 1; m < N; m++)
                {
                    var a = A[n, n];
                    var b = A[n, m];
                    var r = (float)Math.Sqrt(a * a + b * b);
                    if (r < 0.1)
                        angles.Add(new TVector(1, 0));
                    else
                    {
                        var cosA = a / r;
                        var sinA = -b / r;
                        angles.Add(new TVector(cosA, sinA));
                        A.Rotate(n, m, cosA, sinA);
                    }
                    //nRow = cosPhi * R(n,:) – sinPhi * R(m,:);
                    //mRow = sinPhi * R(n,:) + cosPhi * R(m,:);
                    //R(n,:) = nRow;
                    //R(m,:) = mRow;
                    //nCol = cosPhi * Q(:, n) – sinPhi * Q(:, m);
                    //mCol = sinPhi * Q(:, n) + cosPhi * Q(:, m);
                    //Q(:, n) = nCol;
                    //Q(:, m) = mCol;
                }
            return angles;
        }

    };
}

