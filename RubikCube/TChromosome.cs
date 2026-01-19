using TGL.GA.Interfaces;

namespace GA
{
    public class TChromosome : IChromosome, IComparable<TChromosome>
    {
        public static int GenesLength;
        public double[] Genes = new double[GenesLength];
        public static double[] MinGenes = null!;
        public static double[] MaxGenes = null!;
        public double Fitness { get; set; } = double.MaxValue;
        public static Random Rnd = new Random();

        // IChromosome implementation
        double[] IChromosome.Genes => Genes;
        public int Length => GenesLength;

        public TChromosome()
        {
            for (var i = 0; i < GenesLength; i++)
                MutateGene(i);
        }

        public virtual void MutateGene(int idx)
        {
            double ratio = Rnd.NextDouble();
            Genes[idx] = MinGenes[idx] + (MaxGenes[idx] - MinGenes[idx]) * ratio;
        }

        public virtual void Mutate()
        {
            MutateGene(Rnd.Next(GenesLength));
        }

        public virtual TChromosome Crossover(TChromosome other, int splitIdx)
        {
            var child = (TChromosome)Activator.CreateInstance(GetType())!;
            Array.Copy(other.Genes, child.Genes, Genes.Length);
            Array.Copy(Genes, child.Genes, splitIdx);
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
            for (var i = 0; i < GenesLength; i++)
            {
                double ratio = rng.NextDouble();
                Genes[i] = MinGenes[i] + (MaxGenes[i] - MinGenes[i]) * ratio;
            }
        }

        public virtual void Validate()
        {
            // Base implementation does nothing - subclasses can override
        }

        public virtual object Clone()
        {
            var clone = (TChromosome)Activator.CreateInstance(GetType())!;
            Array.Copy(Genes, clone.Genes, Genes.Length);
            clone.Fitness = Fitness;
            return clone;
        }
    }
}
