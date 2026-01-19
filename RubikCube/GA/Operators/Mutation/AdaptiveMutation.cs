using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Adaptive mutation that adjusts mutation intensity based on chromosome fitness.
///
/// The idea is to balance exploration and exploitation:
/// - Poor fitness (high value) → aggressive mutation (more genes, larger changes)
/// - Good fitness (low value) → gentle mutation (fewer genes, smaller changes)
///
/// This allows the algorithm to:
/// - Explore widely when solutions are far from optimal
/// - Refine carefully when solutions are close to optimal
///
/// The mutation intensity is calculated as:
///   intensity = (fitness - minExpectedFitness) / (maxExpectedFitness - minExpectedFitness)
///   genesToMutate = minGenes + intensity * (maxGenes - minGenes)
///
/// For Rubik's cube:
/// - Fitness 0 = solved (best)
/// - Higher fitness = more cubies out of place (worse)
///
/// The operator combines multiple mutation strategies:
/// - High intensity: Scramble mutation (aggressive restructuring)
/// - Medium intensity: Random gene replacement
/// - Low intensity: Neighbor mutation (small angle changes)
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class AdaptiveMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _minGenes;
    private readonly int _maxGenes;
    private readonly double _minExpectedFitness;
    private readonly double _maxExpectedFitness;

    /// <summary>
    /// Creates an adaptive mutation operator.
    /// </summary>
    /// <param name="minGenes">Minimum genes to mutate (for good fitness). Default: 1</param>
    /// <param name="maxGenes">Maximum genes to mutate (for poor fitness). Default: 10</param>
    /// <param name="minExpectedFitness">Expected minimum fitness (best case). Default: 0</param>
    /// <param name="maxExpectedFitness">Expected maximum fitness (worst case). Default: 100</param>
    public AdaptiveMutation(
        int minGenes = 1,
        int maxGenes = 10,
        double minExpectedFitness = 0,
        double maxExpectedFitness = 100)
    {
        _minGenes = Math.Max(1, minGenes);
        _maxGenes = Math.Max(_minGenes, maxGenes);
        _minExpectedFitness = minExpectedFitness;
        _maxExpectedFitness = Math.Max(minExpectedFitness + 1, maxExpectedFitness);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Calculate normalized intensity (0 = best fitness, 1 = worst fitness)
        double intensity = CalculateIntensity(chromosome.Fitness);

        // Determine number of genes to mutate based on intensity
        int genesToMutate = _minGenes + (int)(intensity * (_maxGenes - _minGenes));
        genesToMutate = Math.Min(genesToMutate, length);

        // Choose mutation strategy based on intensity
        if (intensity > 0.7)
        {
            // High intensity: Scramble mutation (aggressive)
            ApplyScrambleMutation(chromosome, rng, genesToMutate);
        }
        else if (intensity > 0.3)
        {
            // Medium intensity: Random gene replacement
            ApplyRandomMutation(chromosome, rng, genesToMutate);
        }
        else
        {
            // Low intensity: Neighbor mutation (gentle)
            ApplyNeighborMutation(chromosome, rng, genesToMutate);
        }
    }

    private double CalculateIntensity(double fitness)
    {
        // Clamp fitness to expected range
        double clampedFitness = Math.Clamp(fitness, _minExpectedFitness, _maxExpectedFitness);

        // Normalize to 0-1 range (0 = best, 1 = worst)
        return (clampedFitness - _minExpectedFitness) / (_maxExpectedFitness - _minExpectedFitness);
    }

    private void ApplyScrambleMutation(T chromosome, Random rng, int intensity)
    {
        int length = chromosome.Length;

        // Scramble a segment proportional to intensity
        int segmentSize = Math.Max(2, Math.Min(intensity * 2, length / 2));
        int start = rng.Next(length - segmentSize);

        // Fisher-Yates shuffle on the segment
        for (int i = segmentSize - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int actualI = start + i;
            int actualJ = start + j;

            (chromosome.Genes[actualI], chromosome.Genes[actualJ]) =
                (chromosome.Genes[actualJ], chromosome.Genes[actualI]);
        }

        // Also do some random replacements
        ApplyRandomMutation(chromosome, rng, intensity / 2);
    }

    private void ApplyRandomMutation(T chromosome, Random rng, int count)
    {
        // For Rubik chromosomes, use valid moves
        if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                int idx = rng.Next(chromosome.Length);
                chromosome.Genes[idx] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
            }
            return;
        }

        // Generic fallback
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(chromosome.Length);
            // Simple random value in reasonable range
            chromosome.Genes[idx] = rng.NextDouble() * 1000;
        }
    }

    private void ApplyNeighborMutation(T chromosome, Random rng, int count)
    {
        // For Rubik chromosomes, change angles (neighbor moves)
        if (chromosome is IRubikChromosome)
        {
            for (int i = 0; i < count; i++)
            {
                int idx = rng.Next(chromosome.Length);
                int moveCode = (int)chromosome.Genes[idx];

                // Decode the move
                var move = TMove.Decode(moveCode);

                // Change angle to a neighbor value
                // Angles: 0=90°, 1=180°, 2=-90°
                int angleChange = rng.Next(2) == 0 ? 1 : 2; // +1 or +2
                move.Angle = (move.Angle + angleChange) % 3;

                chromosome.Genes[idx] = move.Encode();
            }
            return;
        }

        // Generic fallback: small perturbation
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(chromosome.Length);
            double perturbation = (rng.NextDouble() - 0.5) * 10; // Small change
            chromosome.Genes[idx] += perturbation;
        }
    }
}
