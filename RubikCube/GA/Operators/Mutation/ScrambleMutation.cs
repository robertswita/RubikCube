using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Scramble mutation - randomly shuffles a subsequence of genes.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class ScrambleMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        int start = rng.Next(length - 1);
        int end = rng.Next(start + 1, length);
        int segmentLength = end - start;

        if (segmentLength < 2) return;

        // Fisher-Yates shuffle on the segment
        for (int i = segmentLength - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int actualI = start + i;
            int actualJ = start + j;

            (chromosome.Genes[actualI], chromosome.Genes[actualJ]) =
                (chromosome.Genes[actualJ], chromosome.Genes[actualI]);
        }
    }
}
