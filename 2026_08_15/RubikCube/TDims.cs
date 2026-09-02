using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TDims
    {
        int[] Dims;
        int[] Strides;
        int[] BitMasks;
        int[] BitShifts;

        public int Rank => Dims.Length;
        public int TotalLinearSize { get; }
        public int TotalBitSize { get; } // Logic size with padding powers of 2

        public TDims(params int[] dims)
        {
            Dims = (int[])dims.Clone();
            var size = 1;
            Strides = new int[Rank];
            BitMasks = new int[Rank];
            BitShifts = new int[Rank];
            int bitOffset = 0;
            for (int i = 0; i < Rank; i++)
            {
                Strides[i] = size;
                size = Strides[i] * Dims[i];
                int bits = System.Numerics.BitOperations.Log2((uint)(2 * Dims[i] - 1));
                BitMasks[i] = (1 << bits) - 1;
                BitShifts[i] = bitOffset;
                bitOffset += bits;
            }
            TotalLinearSize = size;
            TotalBitSize = 1 << bitOffset;
        }

        public int CoordsToLinear(ReadOnlySpan<int> coords)
        {
            int idx = 0;
            for (int i = 0; i < Rank; i++) idx += coords[i] * Strides[i];
            return idx;
        }

        public int[] LinearToCoords(int idx)
        {
            var c = new int[Rank];
            for (int i = Rank - 1; i >= 0; i--) { c[i] = idx / Strides[i]; idx %= Strides[i]; }
            return c;
        }

        public int CoordsToBitIndex(ReadOnlySpan<int> coords)
        {
            int idx = 0;
            for (int i = 0; i < Rank; i++) 
                idx |= coords[i] << BitShifts[i];
            return idx;
        }

        public int[] BitIndexToCoords(int idx)
        {
            var c = new int[Rank];
            for (int i = 0; i < Rank; i++)
                c[i] = (idx >> BitShifts[i]) & BitMasks[i];
            return c;
        }

        public int this[int axis] => Dims[axis];
    }

}
