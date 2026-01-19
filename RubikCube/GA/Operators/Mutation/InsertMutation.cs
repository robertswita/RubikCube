using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Insert mutation - inserts a neutral move pair at a random position.
/// A neutral pair consists of a move and its inverse (e.g., R R'), which
/// cancel each other out. This allows exploration of longer solution paths
/// without immediately breaking existing progress.
///
/// Since genome length is fixed, this replaces two adjacent genes with the neutral pair.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class InsertMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Pick a random position for the neutral pair
        int idx = rng.Next(length - 1);

        // Get a random move from ValidMoves if available
        TMove move;
        if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
        {
            move = TMove.Decode(rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)]);
        }
        else
        {
            // Fallback: use the move already at this position
            move = TMove.Decode((int)chromosome.Genes[idx]);
        }

        // Create the inverse move
        var inverseMove = move;
        inverseMove.Angle = 2 - move.Angle; // Invert: 0->2, 1->1, 2->0

        // Insert the neutral pair (move followed by its inverse)
        chromosome.Genes[idx] = move.Encode();
        chromosome.Genes[idx + 1] = inverseMove.Encode();
    }
}
