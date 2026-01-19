using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Neighbor mutation - changes a move to a similar one.
/// Changes only the angle of the move (e.g., R -> R' or R -> R2).
/// This is a subtle mutation that preserves the general structure of the solution.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class NeighborMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length == 0) return;

        // Pick a random gene to mutate
        int idx = rng.Next(length);

        // Decode the move
        var move = TMove.Decode((int)chromosome.Genes[idx]);

        // Change the angle to a different value (0, 1, or 2)
        // Current angle is move.Angle, pick one of the other two
        int newAngle;
        if (rng.NextDouble() < 0.5)
        {
            // Invert the angle: 0->2, 1->1, 2->0
            newAngle = 2 - move.Angle;
        }
        else
        {
            // Rotate the angle: 0->1, 1->2, 2->0
            newAngle = (move.Angle + 1) % 3;
        }

        move.Angle = newAngle;
        chromosome.Genes[idx] = move.Encode();
    }
}
