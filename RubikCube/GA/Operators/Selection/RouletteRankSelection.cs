using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Roulette rank selection - probability based on rank rather than raw fitness.
/// Provides more uniform selection pressure.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RouletteRankSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int populationCount = population.Count;
        var ranks = new double[populationCount];
        double rankSum = 0;

        // Use rank instead of fitness (population assumed sorted, best first)
        for (int i = 0; i < populationCount; i++)
        {
            ranks[i] = i; // Rank 0 = best
            rankSum += ranks[i];
        }

        // Reverse so that best individuals (rank 0) have highest cumulative probability
        Array.Reverse(ranks);

        // Build cumulative probability distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = ranks[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + ranks[i];
        }

        // Select using binary search
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double p = rng.NextDouble() * rankSum;
            int first = 0;
            int last = populationCount - 1;

            while (first < last - 1)
            {
                int middle = (last + first) / 2;
                if (p < cumulativeProb[middle])
                    last = middle;
                else
                    first = middle;
            }

            selection.Add(population[first]);
        }

        return selection;
    }
}
