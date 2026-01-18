using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Rank selection - selects the top N individuals by fitness (deterministic).
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RankSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        // Population is assumed to be sorted by fitness (best first)
        return population.Take(count).ToList();
    }
}
