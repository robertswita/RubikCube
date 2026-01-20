using TGL.GA.Interfaces;

namespace GA
{
    /// <summary>
    /// Mock TChromosome base class for testing mutation operators.
    /// </summary>
    public class TChromosome : IChromosome, IComparable<TChromosome>
    {
        public static int GenesLength = 10;
        public double[] Genes = new double[GenesLength];
        public static double[] MinGenes = new double[GenesLength];
        public static double[] MaxGenes = new double[GenesLength];
        public double Fitness { get; set; } = double.MaxValue;
        public static Random Rnd = new Random();

        double[] IChromosome.Genes => Genes;
        public int Length => GenesLength;

        public TChromosome()
        {
            // Initialize with default values
            for (int i = 0; i < GenesLength; i++)
            {
                Genes[i] = i;
            }
        }

        public virtual void MutateGene(int idx)
        {
            Genes[idx] = Rnd.NextDouble() * 100;
        }

        public virtual void Mutate()
        {
            MutateGene(Rnd.Next(GenesLength));
        }

        public virtual TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = new TChromosome();
            Array.Copy(Genes, child.Genes, splitIdx);
            Array.Copy(other.Genes, splitIdx, child.Genes, splitIdx, Genes.Length - splitIdx);
            return child;
        }

        public int CompareTo(TChromosome? other)
        {
            if (other == null) return -1;
            return Fitness.CompareTo(other.Fitness);
        }

        int IComparable<IChromosome>.CompareTo(IChromosome? other)
        {
            if (other == null) return -1;
            return Fitness.CompareTo(other.Fitness);
        }

        public virtual void Randomize(Random rng)
        {
            for (int i = 0; i < GenesLength; i++)
            {
                Genes[i] = rng.NextDouble() * 100;
            }
        }

        public virtual void Validate() { }

        public virtual object Clone()
        {
            var clone = new TChromosome();
            Array.Copy(Genes, clone.Genes, Genes.Length);
            clone.Fitness = Fitness;
            return clone;
        }
    }
}

namespace TGL
{
    /// <summary>
    /// Mock TAffine for testing mutation operators.
    /// Provides static N and Planes properties used by mutation operators.
    /// </summary>
    public class TAffine
    {
        private static int _n = 3;

        public static int N
        {
            get => _n;
            set
            {
                _n = value;
                // Generate planes for N dimensions: all pairs (i,j) where i < j
                Planes = new int[_n * (_n - 1) / 2][];
                var idx = 0;
                for (int colIdx = 1; colIdx < _n; colIdx++)
                    for (int rowIdx = 0; rowIdx < colIdx; rowIdx++)
                    {
                        Planes[idx] = new int[] { rowIdx, colIdx };
                        idx++;
                    }
            }
        }

        public static int[][] Planes { get; private set; } = null!;

        static TAffine()
        {
            N = 3; // Initialize with default 3D
        }
    }
}

namespace RubikCube
{
    /// <summary>
    /// Mock TRubikCube for testing mutation operators.
    /// Provides static Size property and instance methods used by mutation operators.
    /// </summary>
    public class TRubikCube
    {
        public static int Size = 3;

        /// <summary>
        /// Default constructor for creating a new cube.
        /// </summary>
        public TRubikCube() { }

        /// <summary>
        /// Copy constructor for creating a copy of an existing cube.
        /// </summary>
        public TRubikCube(TRubikCube source) { }

        /// <summary>
        /// Apply a move to the cube (mock implementation - does nothing).
        /// </summary>
        public void Turn(TMove move) { }

        /// <summary>
        /// Evaluate the cube state (mock implementation - returns 0).
        /// </summary>
        public double Evaluate() => 0;
    }

    /// <summary>
    /// Mock TMove for testing mutation operators.
    /// Provides Encode/Decode functionality matching the real implementation.
    /// </summary>
    public class TMove
    {
        public int Axis;
        public int Slice;
        public int Plane;
        public int Angle;

        // Dimension sizes for encoding: [N, Size, Planes.Length, 3]
        private static int[] DimSizes => new int[]
        {
            TGL.TAffine.N,
            TRubikCube.Size,
            TGL.TAffine.Planes.Length,
            3
        };

        /// <summary>
        /// Decodes an integer move code into a TMove object.
        /// </summary>
        public static TMove Decode(int code)
        {
            var move = new TMove();

            // Decode from flattened index to multi-dimensional coordinates
            // Order: Axis, Slice, Plane, Angle
            int remaining = code;

            // Decode in reverse order (Angle first, then Plane, Slice, Axis)
            move.Angle = remaining % 3;
            remaining /= 3;

            move.Plane = remaining % TGL.TAffine.Planes.Length;
            remaining /= TGL.TAffine.Planes.Length;

            move.Slice = remaining % TRubikCube.Size;
            remaining /= TRubikCube.Size;

            move.Axis = remaining % TGL.TAffine.N;

            return move;
        }

        /// <summary>
        /// Encodes this TMove into an integer code.
        /// </summary>
        public int Encode()
        {
            // Encode as: ((Axis * Size + Slice) * Planes.Length + Plane) * 3 + Angle
            return ((Axis * TRubikCube.Size + Slice) * TGL.TAffine.Planes.Length + Plane) * 3 + Angle;
        }

        /// <summary>
        /// Gets the axes that define this move's rotation plane.
        /// </summary>
        public int[] GetPlaneAxes()
        {
            return TGL.TAffine.Planes[Plane];
        }

        /// <summary>
        /// Checks if this move is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (Axis >= TGL.TAffine.N) return false;
                if (Plane >= TGL.TAffine.Planes.Length) return false;
                if (Slice < 0 || Slice >= TRubikCube.Size) return false;
                if (Angle < 0 || Angle > 2) return false;
                return true;
            }
        }

        /// <summary>
        /// Gets the inverse of this move (same axis/slice/plane, opposite angle).
        /// </summary>
        public TMove GetInverse()
        {
            return new TMove
            {
                Axis = this.Axis,
                Slice = this.Slice,
                Plane = this.Plane,
                Angle = (3 - this.Angle) % 3 // 0->0, 1->2, 2->1
            };
        }

        /// <summary>
        /// Gets the total number of possible moves for the current configuration.
        /// </summary>
        public static int TotalMoves => TGL.TAffine.N * TRubikCube.Size * TGL.TAffine.Planes.Length * 3;
    }

    /// <summary>
    /// Mock TRubikGenome for testing mutation operators.
    /// </summary>
    public class TRubikGenome : GA.TChromosome, TGL.GA.Interfaces.IRubikChromosome
    {
        public int MovesCount { get; set; }
        public static List<int> FreeMoves = new List<int>();

        IReadOnlyList<int> TGL.GA.Interfaces.IRubikChromosome.ValidMoves
        {
            get => FreeMoves;
            set => FreeMoves = value.ToList();
        }

        static TRubikGenome()
        {
            // Initialize FreeMoves with valid move codes
            for (int i = 0; i < TMove.TotalMoves; i++)
            {
                var move = TMove.Decode(i);
                if (move.IsValid)
                {
                    FreeMoves.Add(i);
                }
            }
        }

        public override void MutateGene(int idx)
        {
            if (FreeMoves.Count > 0)
            {
                Genes[idx] = FreeMoves[Rnd.Next(FreeMoves.Count)];
            }
        }

        public override void Randomize(Random rng)
        {
            for (int i = 0; i < GenesLength; i++)
            {
                if (FreeMoves.Count > 0)
                {
                    Genes[i] = FreeMoves[rng.Next(FreeMoves.Count)];
                }
            }
        }

        public override object Clone()
        {
            var clone = new TRubikGenome();
            Array.Copy(Genes, clone.Genes, Genes.Length);
            clone.Fitness = Fitness;
            clone.MovesCount = MovesCount;
            return clone;
        }
    }
}
