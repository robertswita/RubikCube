using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Unique selection - selects individuals with unique fitness values.
/// Preserves genetic diversity by avoiding over-representation of identical-fitness individuals.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class UniqueSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        var selection = new List<T>(count);

        if (population.Count == 0)
            return selection;

        // Always include the best individual
        selection.Add(population[0]);

        for (int i = 1; i < population.Count && selection.Count < count; i++)
        {
            var specimen = population[i];
            var lastSelected = selection[^1];

            // Add if fitness is different OR we need to fill remaining slots
            int remaining = count - selection.Count;
            int populationRemaining = population.Count - i;

            if (Math.Abs(specimen.Fitness - lastSelected.Fitness) > double.Epsilon ||
                populationRemaining <= remaining)
            {
                selection.Add(specimen);
            }
        }

        return selection;
    }
}
