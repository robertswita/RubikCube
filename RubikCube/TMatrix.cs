using System;
using System.Collections.Generic;

namespace TGL
{
    // Klasa macierzy w zapisie kolumnowym
    public class TMatrix : TVector
    {
        public int ColsCount;
        public int RowsCount;
        public TCols Cols;
        public class TCols
        {
            public TMatrix M;
            public TVector this[int idx]
            {
                get
                {
                    TVector result = new TVector(M.RowsCount);
                    Array.Copy(M.Data, idx * result.Size, result.Data, 0, result.Size);
                    return result;
                }
                set
                {
                    Array.Copy(value.Data, 0, M.Data, idx * M.RowsCount, value.Size);
                }
            }
            public void Swap(int i, int j)
            {
                TVector tmp = M.Cols[i];
                M.Cols[i] = M.Cols[j];
                M.Cols[j] = tmp;
            }
            public void Remove(int idx)
            {
                Array.Copy(M.Data, (idx + 1) * M.RowsCount, M.Data, idx * M.RowsCount, (M.ColsCount - 1 - idx) * M.RowsCount);
                M.ColsCount--;
                Array.Resize(ref M.Data, M.RowsCount * M.ColsCount);
            }
        }
        public TMatrix(int rowsCount, int colsCount) : base(rowsCount * colsCount)
        {
            Cols = new TCols();
            Cols.M = this;
            RowsCount = rowsCount;
            ColsCount = colsCount;
        }
        public override TVector Clone()
        {
            TMatrix result = new TMatrix(RowsCount, ColsCount);
            result.Assign(this);
            return result;
        }
        public TMatrix Transpose()
        {
            TMatrix dest = new TMatrix(ColsCount, RowsCount);
            for (int i = 0; i < RowsCount; i++)
                for (int j = 0; j < ColsCount; j++)
                    dest[j, i] = this[i, j];
            return dest;
        }
        public float this[int row, int col]
        { 
            get { return Data[col * RowsCount + row]; }
            set { Data[col * RowsCount + row] = value; }
        }
        public static TMatrix operator +(TMatrix left, TMatrix right)
        { return (TMatrix)((TVector)left + right); }
        public static TMatrix operator -(TMatrix left, TMatrix right)
        { return (TMatrix)((TVector)left - right); }
        public static TMatrix operator *(TMatrix left, float value)
        { return (TMatrix)((TVector)left * value); }
        public static TMatrix operator *(TMatrix left, TMatrix right)
        {
            TMatrix dest = new TMatrix(left.RowsCount, right.ColsCount);
            for (int i = 0; i < dest.ColsCount; i++)
                dest.Cols[i] = left * right.Cols[i];
            return dest;
        }
        public static TVector operator *(TMatrix m, TVector v)
        {
            TVector dest = new TVector(m.RowsCount);
            for (int i = 0; i < m.ColsCount; i++)
                if (v[i] != 0)
                    dest += m.Cols[i] * v[i];
            return dest;
        }
        public static TVector operator *(TVector v, TMatrix m)
        {
            return m.Transpose() * v;
        }
        float _Det;
        public float Det
        {
            get
            {
                var inv = Inv;
                return _Det;
            }
        }
        // Metodą eliminacji Gaussa-Jordana znajdujemy wynik mnożenia [dest] przez odwrotność [src]  
        //              [result] = [dst] * [src]-1  , czyli
        //              [dst] = [result] * [src]
        // Mnożąc i dodając do siebie kolumny macierzy [src] (i odpowiednio w [dest]), redukujemy 
        // macierz [src] do macierzy jednostkowej. Wówczas zmodyfikowana macierz [dest] 
        // będzie taka sama jak szukana macierz [result]. 
        // Dla [dst] będącej macierzą jednostkową możemy wyznaczyć odwrotność [src]
        public TMatrix Inv
        {
            get
            {
                _Det = 1;
                TMatrix src = (TMatrix)Clone();
                TMatrix dst = new TMatrix(ColsCount, ColsCount);
                dst.LoadIdentity();
                for (int i = 0; i < src.ColsCount; i++)
                {
                    int pivot = i;
                    for (int j = i + 1; j < src.ColsCount; j++)
                        if (Math.Abs(src[i, j]) > Math.Abs(src[i, pivot]))
                            pivot = j;
                    if (pivot != i)
                    {
                        _Det = -_Det;
                        src.Cols.Swap(i, pivot);
                        dst.Cols.Swap(i, pivot);
                    }
                    var tmp = src[i, i];
                    _Det *= tmp;
                    src.Cols[i] *= 1 / tmp;
                    dst.Cols[i] *= 1 / tmp;
                    for (int j = 0; j < src.ColsCount; j++)
                    {
                        tmp = src[i, j];
                        if (j != i && tmp != 0)
                        {
                            src.Cols[j] -= src.Cols[i] * tmp;
                            dst.Cols[j] -= dst.Cols[i] * tmp;
                        }
                    }
                }
                return dst;
            }
        }
        public static TVector Cross(params TVector[] bases)
        {
            var n = bases.Length + 1;
            var A = new TMatrix(n, n - 1);
            for (int i = 0; i < n - 1; i++)
                A.Cols[i] = bases[i];
            A = A.Transpose();
            var result = new TVector(n);
            for (int i = 0; i < n; i++)
            {
                var B = (TMatrix)A.Clone();
                B.Cols.Remove(i);
                result[i] = i % 2 == 0 ? B.Det : -B.Det;
            }
            return result;
        }
        public void LoadIdentity()
        {
            Clear();
            for (int i = 0; i < Math.Min(RowsCount, ColsCount); i++)
                this[i, i] = 1;
        }

        public void Rotate(int i, int j, float cosA, float sinA)
        {
            int colA = i * RowsCount;
            int colB = j * RowsCount;
            for (int n = 0; n < RowsCount; n++)
            {
                float a = Data[colA];
                float b = Data[colB];
                Data[colA] = cosA * a + sinA * b;
                Data[colB] = -sinA * a + cosA * b;
                colA++;
                colB++;
            }
            //var iCol = Cols[i] * cosA - Cols[j] * sinA;
            //var jCol = Cols[i] * sinA + Cols[j] * cosA;
            //Cols[i] = iCol;
            //Cols[j] = jCol;
        }
    };

}
