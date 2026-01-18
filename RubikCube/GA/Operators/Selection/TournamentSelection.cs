using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Tournament selection - randomly picks groups and selects the best from each.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class TournamentSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    private readonly int _tournamentSize;

    /// <summary>
    /// Creates a tournament selection operator.
    /// </summary>
    /// <param name="tournamentSize">Number of individuals competing in each tournament.</param>
    public TournamentSelection(int tournamentSize = 5)
    {
        _tournamentSize = tournamentSize;
    }

    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        var selection = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            int bestIdx = rng.Next(population.Count);

            for (int j = 1; j < _tournamentSize; j++)
            {
                int idx = rng.Next(population.Count);
                if (population[idx].Fitness < population[bestIdx].Fitness)
                {
                    bestIdx = idx;
                }
            }

            selection.Add(population[bestIdx]);
        }

        return selection;
    }
}
