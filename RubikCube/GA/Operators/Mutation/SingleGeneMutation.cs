using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Single gene mutation - changes exactly one random gene to a random valid move.
/// This matches the original TGA behavior where Mutate() changed one gene.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SingleGeneMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        if (chromosome is not TRubikGenome genome)
            return;

        // Change exactly one random gene to a random valid move
        int idx = rng.Next(genome.Length);
        if (TRubikGenome.FreeMoves.Count > 0)
        {
            genome.Genes[idx] = TRubikGenome.FreeMoves[rng.Next(TRubikGenome.FreeMoves.Count)];
        }
    }
}
