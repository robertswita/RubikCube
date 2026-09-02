using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GA
{
    public class TGA<T> where T : TChromosome, new()
    {
        public double HighScore = double.MaxValue;
        public static int PopulationCount = 100;
        public double WinnerRatio = 0.3;
        public double MutationRatio = 0.01;
        public double MutationAuxRatio = 0;
        public int GenerationsCount = 2000;
        public int IterCount;
        public List<T> Population = new List<T>();
        public delegate void ProgressHandler(T best);
        //public delegate double EvaluateHandler(T specimen);
        public delegate void EvaluateHandler(List<T> population);
        public delegate List<T> SelectionHandler(List<T> population, int winnerCount);
        public delegate void NextGenHandler();
        public delegate void GenesisHandler(List<T> population, int count);
        public EvaluateHandler Evaluate;
        public ProgressHandler Progress;
        public SelectionHandler Select;
        public NextGenHandler NextGeneration;
        public GenesisHandler Genesis;

        public enum TSelectionType { Rank, Tournament, Roulette, RouletteRank, Unique };
        public TSelectionType SelectionType;
        public T Best;


        public void Init()
        {
            for (var i = 0; i < PopulationCount; i++)
            {
                var chromosome = new T();
                chromosome.Init();
                Population.Add(chromosome);
            }

        }

        public void Execute()
        {
            //TChromosome.Rnd = new Random();
            //Best = (T)Population[0].Clone();
            var winnerCount = (int)(WinnerRatio * PopulationCount);
            var mutationsCount = (int)(MutationRatio * PopulationCount);
            var mutationsAuxCount = (int)(MutationAuxRatio * PopulationCount);
            Genesis(Population, PopulationCount);
            Best = Population[0];
            while (Best.Fitness >= HighScore && IterCount < GenerationsCount)
            //while (Best.Fitness >= HighScore)
            {
                //foreach (var specimen in Population)
                //    specimen.Evaluate();
                Evaluate(Population);
                Population.Sort();
                if (Progress != null)
                    Progress(Population[0]);
                if (Population[0].Fitness < Best.Fitness)
                {
                    Best = Population[0];
                    //Best = (T)Population[0].Clone();
                }
                var winners = Select(Population, winnerCount);
                //switch (SelectionType)
                //{
                //    case TSelectionType.Rank:
                //        winners = SelectionRank(winnerCount);
                //        break;
                //    case TSelectionType.Unique:
                //        winners = SelectionUnique(winnerCount);
                //        break;
                //    case TSelectionType.Tournament:
                //        winners = SelectionTournament(winnerCount);
                //        break;
                //    case TSelectionType.Roulette:
                //    case TSelectionType.RouletteRank:
                //        winners = SelectionRoulette(winnerCount);
                //        break;
                //}
                Population = new List<T>();
                for (int i = 0; i < PopulationCount / 2; i++)
                {
                    var splitIdx = TChromosome.Rnd.Next(TChromosome.GenesLength);
                    var momIdx = TChromosome.Rnd.Next(winnerCount);
                    var mom = winners[momIdx];
                    winners.RemoveAt(momIdx);
                    var dad = winners[TChromosome.Rnd.Next(winners.Count)];
                    winners.Add(mom);
                    var child = (T)mom.Crossover(dad, splitIdx);
                    Population.Add(child);
                    child = (T)dad.Crossover(mom, splitIdx);
                    Population.Add(child);
                }
                for (int i = 0; i < mutationsCount; i++)
                {
                    var mutant = Population[TChromosome.Rnd.Next(Population.Count)];
                    mutant.Mutate();
                }
                for (int i = 0; i < mutationsAuxCount; i++)
                {
                    var mutant = Population[TChromosome.Rnd.Next(Population.Count)];
                    mutant.MutateAux();
                }
                IterCount++;
                //NextGeneration();
            }
            HighScore = Best.Fitness;
        }

        private List<T> SelectionRank(int winnerCount)
        {
            return Population.GetRange(0, winnerCount);
        }

        private List<T> SelectionTournament(int winnerCount)
        {
            var selection = new List<T>();
            var attendeesCount = (int)(0.1 * PopulationCount);
            for (var i = 0; i < winnerCount; i++)
            {
                int bestIdx = TChromosome.Rnd.Next(Population.Count);
                for (int j = 0; j < attendeesCount; j++)
                {
                    int idx = TChromosome.Rnd.Next(Population.Count);
                    if (Population[idx].Fitness < Population[bestIdx].Fitness)
                        bestIdx = idx;
                }
                selection.Add(Population[bestIdx]);
            }
            return selection;
        }

        private List<T> SelectionRoulette(int winnerCount)
        {
            var fitness = new double[PopulationCount];
            double fitnessSum = 0;
            for (var i = 0; i < fitness.Length; i++)
            {
                fitness[i] = SelectionType == TSelectionType.RouletteRank ? i : Population[i].Fitness;
                fitnessSum += fitness[i];
            }
            Array.Reverse(fitness);
            var pb = new double[PopulationCount];
            for (var i = 1; i < pb.Length; i++)
                pb[i] = pb[i - 1] + fitness[i];
            var selection = new List<T>();
            for (var i = 0; i < winnerCount; i++)
            {
                var p = TChromosome.Rnd.NextDouble() * fitnessSum;
                var first = 0;
                var last = pb.Length - 1;
                while (first < last - 1)
                {
                    var middle = (last + first) / 2;
                    if (p < pb[middle])
                        last = middle;
                    else
                        first = middle;
                }
                selection.Add(Population[first]);
            }
            return selection;
        }

        public List<T> SelectionUnique(int count)
        {
            var sel = new List<T>();
            sel.Add(Population[0]);
            for (int i = 1; i < Population.Count; i++)
            {
                var specimen = Population[i];
                if (specimen.Fitness != sel[sel.Count - 1].Fitness || Population.Count - 1 - i < count - sel.Count)
                {
                    specimen.Correct();
                    sel.Add(specimen);
                }
                if (sel.Count >= count) break;
            }
            return sel;
        }
    }

}
