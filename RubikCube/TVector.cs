using System;
using System.Numerics;

namespace TGL
{
    [Serializable]
    public class TVector
    {
        public float[] Data;
        public int Size { get { return Data.Length; } }
        public TVector(int count = 3)
        {
            Data = new float[count];
        }
        public TVector(params float[] data)
        {
            Data = new float[data.Length];
            Array.Copy(data, Data, data.Length);
        }
        public float this[int idx]
        {
            get { return Data[idx]; }
            set { Data[idx] = value; }
        }
        public void Assign(TVector src)
        {
            Array.Copy(src.Data, Data, Size);
        }
        public void Clear() { Array.Clear(Data, 0, Data.Length); }
        public virtual TVector Clone()
        {
            return new TVector(Data);
        }
        // Operacje sumowania i mnożenia wektorów oraz przekształcenia odwrotne są tak często używane, 
        // że będziemy do tego celu używać operatorów arytmetycznych. Przeciążymy operatory +, -, *, /
        public static TVector operator +(TVector left, TVector right)
        {
            TVector dest = left.Clone();
            int i = 0;
            //if (Vector.IsHardwareAccelerated)
            //{
            //    var simdLen = Vector<float>.Count;
            //    for (; i <= dest.Size - simdLen; i += simdLen)
            //    {
            //        var l = new Vector<float>(left.Data, i);
            //        var r = new Vector<float>(right.Data, i);
            //        Vector.Add(l, r).CopyTo(dest.Data, i);
            //    }
            //}
            for (; i < dest.Size; i++)
                dest[i] += right[i];
            return dest;
        }
        public static TVector operator -(TVector left, TVector right)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] -= right[i];
            return dest;
        }
        public static TVector operator *(TVector left, TVector right)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] *= right[i];
            return dest;
        }
        public static TVector operator /(TVector left, TVector right)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] /= right[i];
            return dest;
        }
        public static TVector operator +(TVector left, float value)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] += value;
            return dest;
        }
        public static TVector operator -(TVector left, float value)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] -= value;
            return dest;
        }
        public static TVector operator *(TVector left, float value)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] *= value;
            return dest;
        }
        public static TVector operator /(TVector left, float value)
        {
            TVector dest = left.Clone();
            for (int i = 0; i < dest.Size; i++)
                dest[i] /= value;
            return dest;
        }
        public float X { get { return Data[0]; } set { Data[0] = (float)value; } }
        public float Y { get { return Data[1]; } set { Data[1] = (float)value; } }
        public float Z { get { return Data[2]; } set { Data[2] = (float)value; } }
        public float W { get { return Data[3]; } set { Data[3] = (float)value; } }
        // Zdefiniujemy również przydatne operacje na dwóch wektorach – iloczyn skalarny (dot product) 
        // i wektorowy (cross product) oraz property X,Y,Z,W i Norm ułatwiające dostęp do składowych
        public float Dot(TVector v)
        {
            float result = 0;
            for (int i = 0; i < Size; i++)
                result += this[i] * v[i];
            return result;
        }
        public TVector Cross(TVector v)
        {
            TVector result = new TVector();
            result.X = Y * v.Z - Z * v.Y;
            result.Y = Z * v.X - X * v.Z;
            result.Z = X * v.Y - Y * v.X;
            return result;
        }
        public double Norm
        {
            get { return Math.Sqrt(Dot(this)); }
            set
            {
                Assign(this * (float)(value / Norm));
            }
        }
        public TVector Abs()
        {
            var result = Clone();
            for (int i = 0; i < result.Data.Length; i++)
                result[i] = Math.Abs(result[i]);
            return result;
        }
        public float Sum()
        {
            var result = 0f;
            for (int i = 0; i < Data.Length; i++)
                result += Data[i];
            return result;
        }

        public static TVector Uniform(int n)
        {
            var result = new TVector(n);
            for (int i = 0; i < n; i++)
                result[i] = i;
            return result;
        }

        public TVector Resample(TVector x, TVector x_)
        {
            var y_ = new TVector(x_.Size);
            var y = this;
            x_ = x_ / x_[x_.Size - 1] * x[x.Size - 1];
            int j = 1;
            for (int i = 0; i < x_.Size; i++)
            {
                while (x[j] < x_[i]) j++;
                var ratio = (x_[i] - x[j - 1]) / (x[j] - x[j - 1]);
                y_[i] = y[j - 1] * (1 - ratio) + y[j] * ratio;
            }
            return y_;
        }


        //public static implicit operator TComplex[] (TVector v)
        //{
        //    var result = new TComplex[v.Size / 2];
        //    for (int i = 0; i < result.Length; i++)
        //    {
        //        result[i].Real = v[i];
        //        result[i].Imag = v[i + result.Length];
        //    }
        //    return result;
        //}

        //public static implicit operator TVector(TComplex[] c)
        //{
        //    var result = new TMatrix(c.Length, 2);
        //    for (int i = 0; i < c.Length; i++)
        //    {
        //        result[i] = c[i].Real;
        //        result[i + c.Length] = c[i].Imag;
        //    }
        //    return result;
        //}

    };
}
