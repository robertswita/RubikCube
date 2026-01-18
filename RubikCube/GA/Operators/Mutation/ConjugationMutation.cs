using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Conjugation mutation - domain-specific mutation for Rubik's cube.
/// Implements the ABA^-1B^-1 conjugation pattern using group theory symmetry.
/// </summary>
/// <typeparam name="T">The chromosome type (must be IRubikChromosome for full functionality).</typeparam>
public class ConjugationMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly Func<int, int> _invertAngle;

    /// <summary>
    /// Creates a conjugation mutation operator.
    /// </summary>
    /// <param name="invertAngle">Function to invert a move angle. Default: angle => 2 - angle</param>
    public ConjugationMutation(Func<int, int>? invertAngle = null)
    {
        _invertAngle = invertAngle ?? (angle => 2 - angle);
    }

    public void Mutate(T chromosome, Random rng)
    {
        // First validate/clean the chromosome
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 2) return;

        // Pick a random position in the first half
        int geneIdx = rng.Next(length / 2);

        // Mirror inverted moves to the second half
        // This creates the conjugation pattern: moves before geneIdx are inverted
        // and placed symmetrically after geneIdx
        for (int i = 1; i <= geneIdx && geneIdx + i < length; i++)
        {
            int sourceIdx = geneIdx - i;
            int targetIdx = geneIdx + i;

            // Get the move and invert its angle
            int moveCode = (int)chromosome.Genes[sourceIdx];

            // Extract angle (assuming angle is encoded in lower bits)
            // This is a simplified version - the actual TRubikGenome uses TMove.Decode
            int angle = moveCode & 3; // Last 2 bits for angle
            int invertedAngle = _invertAngle(angle);
            int invertedMoveCode = (moveCode & ~3) | invertedAngle;

            chromosome.Genes[targetIdx] = invertedMoveCode;
        }
    }
}
