/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej Inteligencji
***********************************************************/
using RubikCube;
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
        public static int BitsPerRow => N <= 4 ? 3 : 4;
        public TMatrix M = new TMatrix(N, N);
        public TVector Origin = new TVector(N);
        public TAffine() { M.LoadIdentity(); }
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
            Rotate(axis1, axis2, cosA, sinA);
        }

        public void Rotate(int axis1, int axis2, float cosA, float sinA)
        {
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

        //public List<TVector> GetEulerAngles2(List<int> order = null, int[] perm = null)
        //{
        //    var angles = new List<TVector>();
        //    var A = (TMatrix)M.Clone();
        //    if (perm == null)
        //    {
        //        perm = new int[M.ColsCount];
        //        for (int i = 0; i < M.ColsCount; i++)
        //            perm[i] = i;
        //    }
        //    else
        //        for (int i = 0; i < A.ColsCount; i++)
        //            A.Cols[i] = M.Cols[perm[i]];
        //    //var invPerm = new int[perm.Length];
        //    //for (int i = 0; i < perm.Length; i++)
        //    //    invPerm[perm[i]] = i;
        //    var orderCount = order != null ? order.Count : Planes.Length;
        //    for (int i = 0; i < orderCount; i++)
        //    {
        //        var plane = order == null ? Planes[i]: Planes[order[i]];
        //        var axis1 = plane[0];
        //        var axis2 = plane[1];
        //        var a = A[axis1, axis1];
        //        var b = A[axis2, axis1];
        //        var r = (float)Math.Sqrt(a * a + b * b);
        //        TVector rot = null;
        //        if (r < 0.1)
        //            rot = new TVector(1, 0);
        //        else
        //        {
        //            var cosA = a / r;
        //            var sinA = b / r;
        //            A.Rotate(axis1, axis2, cosA, -sinA);
        //            rot = new TVector(cosA, sinA, perm[axis1], perm[axis2]);
        //        }
        //        angles.Add(rot);
        //    }

        //    //var scale = new TVector(N);
        //    //for (int i = 0; i < N; i++)
        //    //    scale[i] = (float)A.Cols[0].Norm;
        //    //var error = (A - TAffine.CreateScale(scale).M).Norm;
        //    //if (error > 1E-3)
        //    //    ;
        //    return angles;
        //}

        bool[] GetEulerMask(List<int> order)
        {
            var mask = new bool[Planes.Length];
            var lastAxis = order[Planes.Length - 1];
            for (int i = 0; i < Planes.Length - 1; i++)
            {
                //var planeIdx = Planes[order[i]];
                //var n = planeIdx[0];
                //var m = planeIdx[1];
                //if (n == N - 1 || mask[m, n + 1] && mask[n + 2, n + 1])
                //if (i == Planes.Length - 1 || mask[m, n + 1] || mask[n + 1, n + 1])
                if (order[i] > lastAxis)
                    mask[Planes.Length - 2 - i] = true;
            }
            return mask;
        }

        public List<TVector> GetEulerAngles(List<int> order = null, bool reversed = false, bool nonstandard = false)
        {
            var angles = new List<TVector>();
            var A = reversed ? M.Transpose() : (TMatrix)M.Clone();
            //if (order == null)
            //{
            //    order = new List<int>();
            //    for (int i = 0; i < Planes.Length; i++)
            //        order.Add(i);
            //}
            //else
            //    order = new List<int>(order);
            //if (reversed)
            //    order.Reverse();
            //var mask = GetEulerMask(order);
            //var A = (TMatrix)M.Clone();
            var orderCount = order != null ? order.Count : Planes.Length;
            for (int i = 0; i < orderCount; i++)
            {
                var plane = order == null ? Planes[i]: Planes[order[i]];
                var axis1 = plane[0];
                var axis2 = plane[1];
                var a = A[axis1, axis1];
                var b = A[axis2, axis1];
                //if (mask[i])
                //{
                //    a = A[axis2, axis2];
                //    b = -A[axis1, axis2];
                //}
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
      //  scale[i] = (float)A.Cols[0].Norm;
      //var error = (A - TAffine.CreateScale(scale).M).Norm;
      //if (error > 1E-3)
      //  ;
      return angles;
        }

        //public uint OrthoPack2()
        //{
        //    int bitsForCol = TAffine.N <= 4 ? 2 : 3;
        //    int bitsPerRow = bitsForCol + 1;
        //    uint m = 0;
        //    for (int row = 0; row < TAffine.N; row++)
        //    {
        //        int nzCol = 0;
        //        float nz = 0;
        //        for (int col = 0; col < TAffine.N; col++)
        //            if (Math.Abs(M[row, col]) > Math.Abs(nz)) { nz = M[row, col]; nzCol = col; }
        //        uint sign = nz < 0 ? 1u : 0u;
        //        m |= ((sign << bitsForCol) | (uint)nzCol) << (row * bitsPerRow);
        //    }
        //    return m;
        //}

        public uint OrthoPack()
        {
            uint m = 0;
            for (int row = 0; row < TAffine.N; row++)
            {
                int nzCol = 0;
                float nz = 0;
                for (int col = 0; col < TAffine.N; col++)
                    if (Math.Abs(M[row, col]) > Math.Abs(nz)) { nz = M[row, col]; nzCol = col; }
                uint sign = nz < 0 ? 1u : 0u;
                uint rowData = ((uint)nzCol << 1) | sign;
                m |= rowData << (row * BitsPerRow);
            }
            return m;
        }

        // Inverse of OrthoPack: rebuilds the orientation block M (a signed permutation) from its packed form. Row r
        // holds its nonzero column (bitsForCol bits) + sign (1 bit) at offset r*bitsPerRow; zero the row and drop the
        // single +-1. Origin/translation is NOT touched (OrthoPack never captured it). Decode mirrors getRow in
        // Setup.glsl.c, so OrthoPack() == p after OrthoUnpack(p) for every valid p.
        //public void OrthoUnpack(uint packed)
        //{
        //    int bitsForCol = TAffine.N <= 4 ? 2 : 3;
        //    int bitsPerRow = bitsForCol + 1;
        //    uint colMask = (1u << bitsForCol) - 1u;
        //    for (int row = 0; row < TAffine.N; row++)
        //    {
        //        uint rowData = (packed >> (row * bitsPerRow)) & ((1u << bitsPerRow) - 1u);
        //        int nzCol = (int)(rowData & colMask);
        //        int sign = ((rowData >> bitsForCol) & 1u) == 1u ? -1 : 1;
        //        for (int col = 0; col < TAffine.N; col++) M[row, col] = 0f;
        //        M[row, nzCol] = sign;
        //    }
        //}
        public void OrthoUnpack(uint packed)
        {
            int bitsPerRow = TAffine.BitsPerRow;
            uint rowMask = (1u << bitsPerRow) - 1u;
            for (int row = 0; row < TAffine.N; row++)
            {
                uint rowData = (packed >> (row * bitsPerRow)) & rowMask;
                int nzCol = (int)(rowData >> 1);
                int sign = (rowData & 1u) == 1u ? -1 : 1;
                for (int col = 0; col < TAffine.N; col++) M[row, col] = 0f;
                M[row, nzCol] = sign;
            }
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

        // Struktura danych wejściowych/wyjściowych bez alokacji obiektowych
        // Wszystkie tablice są płaskimi strukturami sbyte/int przeznaczonymi dla GPU/SIMD
        public unsafe static void GetEulerAnglesPackedKernel(
            sbyte[] populationM,       // Płaska tablica SOA [PopSize * n * n * N]
            int popId,                 // ID aktualnie przetwarzanego osobnika
            int cubeId,                // ID aktualnie przetwarzanego kubika
            int n, int N,              // Wymiar n, całkowita liczba kubików N
            int[][] planes,            // Definicje płaszczyzn obrotu (statyczne, znane z góry)
            int[] order,               // Kolejność płaszczyzn
            byte[] outputStateHashes)  // Tablica wyjściowa na unikalne 2-bitowe hashe stanów kubika
        {
            unchecked
            {
                // 1. Kopiujemy macierz orientacji tego konkretnego kubika do pamięci lokalnej (rejestrów wątku)
                // Maksymalny wymiar n rzadko przekracza 6, więc tablica lokalna n*n zajmie zaledwie 36 bajtów.
                // Na GPU ta pamięć wyląduje w błyskawicznych rejestrach (Local Memory).
                sbyte* localA = stackalloc sbyte[n * n];
                int baseOffset = (popId * n * n * N) + cubeId;

                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        // Pobieramy dane ze struktury SOA do lokalnego bufora roboczego
                        localA[i * n + j] = populationM[baseOffset + (i * n * N) + (j * N)];
                    }
                }

                // Zmienna do spakowania unikalnego hasha stanu kubika (zapis po 2 bity na kąt)
                ulong packedStateHash = 0;
                int bitShift = 0;

                int orderCount = order != null ? order.Length : planes.Length;

                // 2. Główna pętla rozkładu Givensa
                for (int i = 0; i < orderCount; i++)
                {
                    int planeIdx = order == null ? i : order[i];
                    int axis1 = planes[planeIdx][0];
                    int axis2 = planes[planeIdx][1];

                    // Pobieramy wartości z lokalnej macierzy roboczej
                    sbyte a = localA[axis1 * n + axis1];
                    sbyte b = localA[axis2 * n + axis1];

                    sbyte cosA = 1;
                    sbyte sinA = 0;

                    // Eliminujemy Math.Sqrt i operacje float. Dla superkostki wartości a i b mówią nam wszystko:
                    if (a == 0 && b == 0)
                    {
                        cosA = 1;
                        sinA = 0;
                    }
                    else
                    {
                        // Ponieważ a*a + b*b zawsze daje 1 dla poprawnej macierzy orientacji:
                        cosA = a;
                        sinA = b;

                        // Wykonujemy operację A.Rotate bezpośrednio na lokalnej macierzy 8-bitowej.
                        // Obracamy tylko wiersze axis1 i axis2
                        for (int col = 0; col < n; col++)
                        {
                            sbyte origA = localA[axis1 * n + col];
                            sbyte origB = localA[axis2 * n + col];

                            // Zoptymalizowane branchless mnożenie macierzy 8-bitowych
                            localA[axis1 * n + col] = (sbyte)(origA * cosA + origB * sinA);
                            localA[axis2 * n + col] = (sbyte)(-origA * sinA + origB * cosA);
                        }
                    }

                    // 3. Pakowanie stanu do unikalnego klucza (Twoje 2 bity na stan cosA/sinA)
                    // Mapujemy kombinacje (cos, sin) na unikalną wartość 2-bitową (0, 1, 2, 3)
                    byte angleCode = 0;
                    if (cosA == 1 && sinA == 0) angleCode = 0; // 0 stopni
                    if (cosA == 0 && sinA == 1) angleCode = 1; // 90 stopni
                    if (cosA == -1 && sinA == 0) angleCode = 2; // 180 stopni
                    if (cosA == 0 && sinA == -1) angleCode = 3; // 270 stopni

                    // Wstrzykujemy 2 bity do głównego hasha stanu tego kubika
                    packedStateHash |= ((ulong)angleCode << bitShift);
                    bitShift += 2;
                }

                // 4. Zapisujemy unikalny stan (hash) kubika do płaskiej tablicy wyjściowej
                int outputIdx = (popId * N) + cubeId;
                outputStateHashes[outputIdx] = (byte)packedStateHash;
            }
        }


    };

}

