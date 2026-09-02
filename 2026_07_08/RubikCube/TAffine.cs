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

public static class SimpleHypercubeOrientationCoder
    {
        // Statyczna tablica silni dla szybkiego dostępu (obsługuje wymiary do N=12)
        private static int[] Factorials = { 1, 1, 2, 6, 24, 120, 720, 5040, 40320, 362880, 3628800, 39916800, 479001600 };

        /// <summary>
        /// Koduje macierz obrotu do indeksu, zapisując WSZYSTKIE N bitów znaków.
        /// </summary>
        public static int MatrixToIndex(int[,] matrix)
        {
            int n = matrix.GetLength(0);

            Span<int> permutation = stackalloc int[n];
            int signIndex = 0;

            // 1. Jednoczesne wyciąganie permutacji i pakowanie WSZYSTKIECH N znaków jako bity
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        permutation[i] = j;
                        if (matrix[i, j] > 0)
                        {
                            signIndex |= (1 << i); // Ustaw bit na 1 dla znaku dodatniego (+)
                        }
                        break;
                    }
                }
            }

            // 2. Indeksowanie permutacji (Kod Lehmera)
            int permIndex = 0;
            Span<bool> used = stackalloc bool[n];

            for (int i = 0; i < n; i++)
            {
                int currentVal = permutation[i];
                int smallerCount = 0;

                for (int j = 0; j < currentVal; j++)
                {
                    if (!used[j]) smallerCount++;
                }

                permIndex += smallerCount * Factorials[n - 1 - i];
                used[currentVal] = true;
            }

            // 3. Łączenie: Przesuwamy permutację o N bitów (zamiast N-1) i doklejamy znaki
            return (permIndex << n) | signIndex;
        }

        /// <summary>
        /// Dekoduje indeks w sposób uproszczony – bez liczenia wyznacznika/parzystości.
        /// </summary>
        public static int[,] IndexToMatrix(int index, int n)
        {
            int[,] matrix = new int[n, n];

            // Rozdzielenie bitowe na podstawie pełnego wymiaru N
            int signMask = (1 << n) - 1;
            int signIndex = index & signMask;
            int permIndex = (int)((uint)index >> n);

            // 1. Odtworzenie permutacji z kodu Lehmera (liniowy krok)
            Span<int> permutation = stackalloc int[n];
            Span<int> availableCols = stackalloc int[n];
            for (int i = 0; i < n; i++) availableCols[i] = i;

            int tempPerm = permIndex;
            for (int i = 0; i < n; i++)
            {
                int factVal = Factorials[n - 1 - i];
                int choiceIndex = tempPerm / factVal;
                tempPerm %= factVal;

                permutation[i] = availableCols[choiceIndex];
                for (int j = choiceIndex; j < n - 1 - i; j++)
                {
                    availableCols[j] = availableCols[j + 1];
                }
            }

            // 2. Bezpośrednie wpisanie wartości do macierzy na podstawie bitów znaków
            for (int i = 0; i < n; i++)
            {
                int sign = ((signIndex & (1 << i)) != 0) ? 1 : -1;
                matrix[i, permutation[i]] = sign;
            }

            return matrix;
        }
    }

}

