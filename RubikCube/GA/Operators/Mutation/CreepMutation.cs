using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Creep mutation - makes small incremental changes to genes.
///
/// Unlike random mutation which can make large jumps in the solution space,
/// creep mutation makes small, gradual changes. This is useful for fine-tuning
/// solutions that are already close to optimal.
///
/// For continuous optimization:
/// - Adds small random values to genes
/// - Changes are typically within a small range (e.g., ±1-5)
///
/// For Rubik's cube (discrete moves):
/// - Changes move angles incrementally (90° → 180° → 270°/−90°)
/// - Changes slice numbers by ±1 (for larger cubes)
/// - Changes to "adjacent" moves (similar axis/plane)
///
/// This mutation is inspired by evolution strategies where small mutations
/// accumulate over generations to refine solutions.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class CreepMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _genesToMutate;
    private readonly double _creepRange;

    /// <summary>
    /// Creates a creep mutation operator.
    /// </summary>
    /// <param name="genesToMutate">Number of genes to apply creep to. Default: 1</param>
    /// <param name="creepRange">Maximum creep value for continuous genes. Default: 1.0</param>
    public CreepMutation(int genesToMutate = 1, double creepRange = 1.0)
    {
        _genesToMutate = Math.Max(1, genesToMutate);
        _creepRange = Math.Abs(creepRange);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 1) return;

        for (int i = 0; i < _genesToMutate; i++)
        {
            int idx = rng.Next(length);

            if (chromosome is IRubikChromosome)
            {
                // For Rubik's cube: make small discrete changes
                ApplyDiscreteCreep(chromosome, idx, rng);
            }
            else
            {
                // For continuous optimization: add small random value
                ApplyContinuousCreep(chromosome, idx, rng);
            }
        }
    }

    private void ApplyDiscreteCreep(T chromosome, int idx, Random rng)
    {
        int moveCode = (int)chromosome.Genes[idx];
        var move = TMove.Decode(moveCode);

        // Choose what to creep: angle, slice, or axis
        int creepType = rng.Next(3);

        switch (creepType)
        {
            case 0:
                // Creep angle by ±1 (wrapping: 0→1→2→0)
                int angleDelta = rng.Next(2) == 0 ? 1 : 2; // +1 or -1 (mod 3)
                move.Angle = (move.Angle + angleDelta) % 3;
                break;

            case 1:
                // Creep slice by ±1 (if cube size allows)
                int sliceDelta = rng.Next(2) == 0 ? 1 : -1;
                int newSlice = move.Slice + sliceDelta;
                if (newSlice >= 0 && newSlice < TRubikCube.Size)
                {
                    move.Slice = newSlice;
                }
                break;

            case 2:
                // Creep to adjacent plane (if available)
                int planeDelta = rng.Next(2) == 0 ? 1 : -1;
                int planeCount = TAffine.Planes.Length;
                int newPlane = (move.Plane + planeDelta + planeCount) % planeCount;
                move.Plane = newPlane;
                break;
        }

        chromosome.Genes[idx] = move.Encode();
    }

    private void ApplyContinuousCreep(T chromosome, int idx, Random rng)
    {
        // Add small random value in range [-creepRange, +creepRange]
        double creep = (rng.NextDouble() * 2 - 1) * _creepRange;
        chromosome.Genes[idx] += creep;
    }
}
