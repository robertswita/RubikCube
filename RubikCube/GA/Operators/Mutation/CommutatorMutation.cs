using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Commutator mutation - domain-specific mutation for Rubik's cube.
/// Implements the ABA'B' commutator pattern using group theory.
/// Commutators affect only a small number of pieces, making them useful for precise solving.
/// </summary>
/// <typeparam name="T">The chromosome type (must be IRubikChromosome for full functionality).</typeparam>
public class CommutatorMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 4) return; // Need at least 4 positions for ABA'B'

        // Pick a random starting position (need room for 4 moves: A, B, A', B')
        int startIdx = rng.Next(length - 3);

        // Get move A from position startIdx
        var moveA = TMove.Decode((int)chromosome.Genes[startIdx]);

        // Get move B from position startIdx + 1
        var moveB = TMove.Decode((int)chromosome.Genes[startIdx + 1]);

        // Create A' (inverse of A) - invert the angle
        var moveAInverse = moveA;
        moveAInverse.Angle = 2 - moveA.Angle; // Invert: 0->2, 1->1, 2->0

        // Create B' (inverse of B) - invert the angle
        var moveBInverse = moveB;
        moveBInverse.Angle = 2 - moveB.Angle;

        // Apply the commutator pattern: A B A' B'
        // Position 0: A (already there)
        // Position 1: B (already there)
        // Position 2: A'
        chromosome.Genes[startIdx + 2] = moveAInverse.Encode();
        // Position 3: B'
        chromosome.Genes[startIdx + 3] = moveBInverse.Encode();
    }
}
