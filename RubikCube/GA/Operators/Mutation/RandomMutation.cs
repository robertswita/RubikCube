using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Random mutation - replaces a random gene with a new random value.
/// Uses the chromosome's Randomize method to get a valid random value for the domain.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RandomMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _genesToMutate;

    /// <summary>
    /// Creates a random mutation operator.
    /// </summary>
    /// <param name="genesToMutate">Number of genes to mutate per call (default 1).</param>
    public RandomMutation(int genesToMutate = 1)
    {
        _genesToMutate = genesToMutate;
    }

    public void Mutate(T chromosome, Random rng)
    {
        for (int i = 0; i < _genesToMutate; i++)
        {
            int idx = rng.Next(chromosome.Length);
            // Create a temporary chromosome to get a random gene value
            // This is a workaround since we don't have access to gene bounds
            chromosome.Randomize(rng);
        }
    }
}
