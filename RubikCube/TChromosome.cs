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
        public float[] Genes = new float[GenesLength];
        public static float[] MinGenes;
        public static float[] MaxGenes;
        public float Fitness = float.MaxValue;
        public static Random Rnd = new Random();

        //public TChromosome()
        //{
        //    for (var i = 0; i < GenesLength; i++)
        //        MutateGene(i);
        //}

        public virtual void Init()
        {
            for (var i = 0; i < GenesLength; i++)
                MutateGene(i);
        }

        public virtual TChromosome Clone()
        {
            var clone = (TChromosome)Activator.CreateInstance(GetType());
            Array.Copy(Genes, clone.Genes, Genes.Length);
            clone.Fitness = Fitness;
            return clone;
        }

        public virtual void Correct() { }

        public virtual void MutateGene(int idx)
        {
            float ratio = (float)Rnd.NextDouble();
            Genes[idx] = MinGenes[idx] + (MaxGenes[idx] - MinGenes[idx]) * ratio;
        }

        public virtual void Mutate()
        {
            MutateGene(Rnd.Next(GenesLength));
        }

        public virtual void MutateAux()
        {
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

        public virtual float Evaluate() { return Fitness; }

    }
}
