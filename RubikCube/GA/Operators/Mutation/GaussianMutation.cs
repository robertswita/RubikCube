using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Gaussian mutation - adds Gaussian (normal) distributed noise to genes.
///
/// This mutation adds random values drawn from a normal distribution N(0, σ²).
/// The standard deviation (sigma) controls the mutation strength:
/// - Small σ: Fine-grained local search
/// - Large σ: More exploratory mutations
///
/// For continuous optimization:
/// - Directly adds Gaussian noise to gene values
/// - Commonly used in evolution strategies (ES) and CMA-ES
///
/// For Rubik's cube (discrete moves):
/// - Uses Gaussian probability to determine mutation intensity
/// - Higher |noise| = more likely to make bigger changes
/// - Can change multiple aspects of a move based on noise magnitude
///
/// The Gaussian distribution is generated using the Box-Muller transform.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class GaussianMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _genesToMutate;
    private readonly double _sigma;

    /// <summary>
    /// Creates a Gaussian mutation operator.
    /// </summary>
    /// <param name="genesToMutate">Number of genes to mutate. Default: 1</param>
    /// <param name="sigma">Standard deviation of the Gaussian distribution. Default: 1.0</param>
    public GaussianMutation(int genesToMutate = 1, double sigma = 1.0)
    {
        _genesToMutate = Math.Max(1, genesToMutate);
        _sigma = Math.Abs(sigma);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 1) return;

        for (int i = 0; i < _genesToMutate; i++)
        {
            int idx = rng.Next(length);
            double noise = GenerateGaussian(rng) * _sigma;

            if (chromosome is IRubikChromosome rubikChromosome)
            {
                // For Rubik's cube: use noise magnitude to determine mutation intensity
                ApplyDiscreteGaussian(chromosome, rubikChromosome, idx, noise, rng);
            }
            else
            {
                // For continuous optimization: add Gaussian noise directly
                chromosome.Genes[idx] += noise;
            }
        }
    }

    private void ApplyDiscreteGaussian(T chromosome, IRubikChromosome rubikChromosome, int idx, double noise, Random rng)
    {
        double absNoise = Math.Abs(noise);

        if (absNoise > 2.0)
        {
            // Large noise: complete gene replacement
            if (rubikChromosome.ValidMoves.Count > 0)
            {
                chromosome.Genes[idx] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
            }
        }
        else if (absNoise > 1.0)
        {
            // Medium noise: change multiple move components
            int moveCode = (int)chromosome.Genes[idx];
            var move = TMove.Decode(moveCode);

            // Change angle
            move.Angle = (move.Angle + (noise > 0 ? 1 : 2)) % 3;

            // Maybe also change slice
            if (absNoise > 1.5 && TRubikCube.Size > 2)
            {
                int sliceDelta = noise > 0 ? 1 : -1;
                move.Slice = Math.Clamp(move.Slice + sliceDelta, 0, TRubikCube.Size - 1);
            }

            chromosome.Genes[idx] = move.Encode();
        }
        else
        {
            // Small noise: only change angle
            int moveCode = (int)chromosome.Genes[idx];
            var move = TMove.Decode(moveCode);
            move.Angle = (move.Angle + (noise > 0 ? 1 : 2)) % 3;
            chromosome.Genes[idx] = move.Encode();
        }
    }

    /// <summary>
    /// Generates a random number from standard normal distribution N(0,1)
    /// using the Box-Muller transform.
    /// </summary>
    private static double GenerateGaussian(Random rng)
    {
        // Box-Muller transform
        double u1 = 1.0 - rng.NextDouble(); // Uniform(0,1] to avoid log(0)
        double u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
