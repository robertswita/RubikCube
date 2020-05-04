using System;

namespace Rubik
{
    public class TChromosome
    {
        public static int GenesLength = 100;
        public static double[] MinGenes = new double[GenesLength];
        public static double[] MaxGenes = new double[GenesLength];
        public static Random Rnd = new Random();
        public double[] Genes = new double[GenesLength];
        public double UnFitness;

        public TChromosome()
        {
            for (var i = 0; i < GenesLength; i++)
            {
                Mutation(i);
            }
        }

        public virtual TChromosome Cross(TChromosome other, int splitIdx)
        {
            var child = new TChromosome();
            Array.Copy(Genes, 0, child.Genes, 0, splitIdx);
            Array.Copy(
                other.Genes, 
                splitIdx + 1, 
                child.Genes, 
                splitIdx + 1, 
                other.Genes.Length - splitIdx - 1
            );
            return child;
        }

        public virtual void Mutation(int idx)
        {
            Genes[idx] = Rnd.NextDouble() * (MaxGenes[idx] - MinGenes[idx]) + MinGenes[idx];
        }
    }
}