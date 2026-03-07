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
    public class TAffine
    {
        static int n;
        public static int N
        {
            get { return n; }
            set
            {
                n = value;
                Planes = new int[n * (n - 1) / 2][];
                var idx = 0;
                for (int row = 1; row < n; row++)
                    for (int col = 0; col < row; col++)
                    //for (int row = 0; row < n - 1; row++)
                    //    for (int col = row + 1; col < n; col++)
                    {
                        Planes[idx] = new int[] { col, row };
                        idx++;
                    }
            }
        }
        public TMatrix M = new TMatrix(N, N);
        public TVector Origin = new TVector(N);
        public TAffine() { M.LoadIdentity(); }
        //public TAffine(TMatrix src) : base(N + 1, N + 1) { Assign(src); }
        public static int[][] Planes;// = new int[N * (N - 1) / 2][];
        static TAffine()
        {
            N = 3;
            //var idx = 0;
            //for (int i = 0; i < N - 1; i++)
            //    for (int j = i + 1; j < N; j++)
            //    {
            //        Planes[idx] = new int[] { i, j };
            //        idx++;
            //    }
        }

        public static TAffine CreateScale(TVector scale)
        {
            var S = new TAffine();
            for (int i = 0; i < N; i++)
                S.M[i, i] = scale[i];
            return S;
        }

        public static TAffine CreateShear(TVector h)
        {
            var H = new TAffine();
            for (int i = 0; i < Planes.Length; i++)
                H.M[Planes[i][0], Planes[i][1]] = h[i];
            return H;
        }

        public static TAffine CreateRotation(int axis1, int axis2, double angle)
        {
            angle *= Math.PI / 180;
            var cosA = (float)Math.Cos(angle);
            var sinA = (float)Math.Sin(angle);
            var R = new TAffine();
            R.M[axis1, axis1] = cosA;
            R.M[axis2, axis1] = sinA;
            R.M[axis1, axis2] = -sinA;
            R.M[axis2, axis2] = cosA;
            return R;
        }

        public void Rotate(int axis1, int axis2, double angle)
        {
            angle *= Math.PI / 180;
            var cosA = (float)Math.Cos(angle);
            var sinA = (float)Math.Sin(angle);
            M.Rotate(axis1, axis2, cosA, sinA);
            Origin.Rotate(axis1, axis2, cosA, sinA);
        }

        public void Rotate(int plane, double angle)
        {
            Rotate(Planes[plane][0], Planes[plane][1], angle);
        }

        public static TAffine CreateRotation(int plane, double angle)
        {
            return CreateRotation(Planes[plane][0], Planes[plane][1], angle);
        }

        //public static TAffine CreateTranslation(TVector t)
        //{
        //    var T = new TAffine();
        //    T.Origin.Assign(t);
        //    //for (int i = 0; i < N; i++)
        //    //    T[i, N] = t[i];
        //    return T;
        //}

        public TAffine Clone()
        {
            TAffine result = new TAffine();
            result.M.Assign(M);
            result.Origin.Assign(Origin);
            return result;
        }

        public static TAffine operator *(TAffine left, TAffine right)
        {
            var result = new TAffine();
            result.M = left.M * right.M;
            result.Origin = left * right.Origin;
            return result;
        }

        public static TVector operator *(TAffine left, TVector right)
        {
            return left.M * right + left.Origin;
        }

        public List<TVector> GetEulerAngles(List<int> order = null, bool reversed = false)
        {
            var angles = new List<TVector>();
            var A = reversed ? M.Transpose() : (TMatrix)M.Clone();
            for (int i = 0; i < Planes.Length; i++)
            {
                var plane = order == null ? Planes[i] : Planes[order[i]];
                var axis1 = plane[0];
                var axis2 = plane[1];
                var a = A[axis1, axis1];
                var b = A[axis2, axis1];
                var r = (float)Math.Sqrt(a * a + b * b);
                TVector rot = null;
                if (r < 0.1)
                    rot = new TVector(1, 0);
                else
                {
                    var cosA = a / r;
                    var sinA = b / r;
                    A.Rotate(axis1, axis2, cosA, -sinA);
                    rot = new TVector(cosA, sinA, axis1, axis2);
                }
                if (reversed)
                {
                    rot[1] = -rot[1];
                    angles.Insert(0, rot);
                }
                else
                    angles.Add(rot);
            }

            //var scale = new TVector(N);
            //for (int i = 0; i < N; i++)
            //    scale[i] = (float)A.Cols[0].Norm;
            //var error = (A - TAffine.CreateScale(scale).M).Norm;
            //if (error > 1E-3)
            //    ;
            return angles;
        }


        //public List<TVector> GetReversedEulerAngles()
        //{
        //    var A = (TMatrix)M.Clone();
        //    M = M.Transpose();
        //    var angles = GetEulerAngles();
        //    M = A;
        //    for (int i = 0; i < angles.Count; i++)
        //        angles[i][1] *= -1;
        //    return angles;
        //}

    };
}

