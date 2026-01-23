using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA
{
    public class TChromosome : IComparable<TChromosome>
    {
        public static int GenesLength;
        public double[] Genes = new double[GenesLength];
        public static double[] MinGenes;
        public static double[] MaxGenes;
        public double Fitness = double.MaxValue;
        public static Random Rnd = new Random();

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
            var child = (TChromosome)Activator.CreateInstance(GetType());// new TChromosome();
            Array.Copy(other.Genes, child.Genes, Genes.Length);
            Array.Copy(Genes, child.Genes, splitIdx);
            //Array.Copy(other.Genes, splitIndex, child.Genes, splitIndex, Genes.Length - splitIndex);
            return child;
        }

        public int CompareTo(TChromosome other)
        {
            return Fitness.CompareTo(other.Fitness);
        }

        public virtual double Evaluate() { return Fitness; }
    }
}
