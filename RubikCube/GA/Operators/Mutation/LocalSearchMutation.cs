using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Local search mutation - performs hill-climbing on a small neighborhood.
///
/// This mutation tries multiple small modifications and keeps the best one
/// based on a heuristic evaluation. The modifications tried include:
/// - Changing the angle of a single move
/// - Replacing a move with a similar one
/// - Removing redundant move pairs
/// - Simplifying consecutive moves
///
/// The "best" modification is selected using a heuristic score that considers:
/// - Cancellation opportunities (moves that can be simplified)
/// - Sequence coherence (similar moves grouped together)
/// - Redundancy (fewer moves that cancel out)
///
/// For full fitness-based hill-climbing, use with SetBaseCube() which enables
/// actual fitness evaluation during local search.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class LocalSearchMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private TRubikCube? _baseCube;
    private readonly int _neighborhoodSize;
    private readonly int _maxIterations;

    /// <summary>
    /// Creates a local search mutation with default parameters.
    /// </summary>
    public LocalSearchMutation() : this(5, 3) { }

    /// <summary>
    /// Creates a local search mutation with specified parameters.
    /// </summary>
    /// <param name="neighborhoodSize">Number of candidate modifications to try per iteration.</param>
    /// <param name="maxIterations">Maximum iterations of local search.</param>
    public LocalSearchMutation(int neighborhoodSize = 5, int maxIterations = 3)
    {
        _neighborhoodSize = neighborhoodSize;
        _maxIterations = maxIterations;
    }

    /// <summary>
    /// Sets the base cube for fitness-based evaluation.
    /// When set, local search uses actual fitness; otherwise uses heuristics.
    /// </summary>
    public void SetBaseCube(TRubikCube baseCube)
    {
        _baseCube = baseCube;
    }

    public void Mutate(T chromosome, Random rng)
    {
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 2) return;

        // Perform multiple iterations of local search
        for (int iter = 0; iter < _maxIterations; iter++)
        {
            // Generate neighborhood of candidate modifications
            var candidates = GenerateCandidates(chromosome, rng);

            if (candidates.Count == 0) continue;

            // Evaluate candidates and find best
            double bestScore = EvaluateChromosome(chromosome);
            double[] bestGenes = null!;

            foreach (var candidate in candidates)
            {
                double score = EvaluateCandidate(chromosome, candidate);
                if (score < bestScore) // Lower is better (minimization)
                {
                    bestScore = score;
                    bestGenes = candidate;
                }
            }

            // Apply best modification if it improves
            if (bestGenes != null)
            {
                for (int i = 0; i < length; i++)
                {
                    chromosome.Genes[i] = bestGenes[i];
                }
            }
            else
            {
                // No improvement found, stop early
                break;
            }
        }
    }

    /// <summary>
    /// Generate candidate modifications in the neighborhood.
    /// </summary>
    private List<double[]> GenerateCandidates(T chromosome, Random rng)
    {
        var candidates = new List<double[]>();
        int length = chromosome.Length;

        for (int c = 0; c < _neighborhoodSize; c++)
        {
            var candidate = new double[length];
            Array.Copy(chromosome.Genes, candidate, length);

            // Choose a random modification type
            int modType = rng.Next(5);

            switch (modType)
            {
                case 0:
                    // Change angle of a random move
                    ChangeAngle(candidate, rng);
                    break;

                case 1:
                    // Try to simplify adjacent moves
                    SimplifyAdjacent(candidate, rng);
                    break;

                case 2:
                    // Replace with neighbor move (same plane, different angle)
                    ReplaceWithNeighbor(candidate, rng);
                    break;

                case 3:
                    // Try to remove cancel-out pairs
                    RemoveCancelPair(candidate, rng, chromosome);
                    break;

                case 4:
                    // Swap with a "better" move from valid moves
                    SwapWithValid(candidate, rng, chromosome);
                    break;
            }

            candidates.Add(candidate);
        }

        return candidates;
    }

    /// <summary>
    /// Change the angle of a random move.
    /// </summary>
    private static void ChangeAngle(double[] genes, Random rng)
    {
        int idx = rng.Next(genes.Length);
        var move = TMove.Decode((int)genes[idx]);

        // Try a different angle
        int newAngle = (move.Angle + 1 + rng.Next(2)) % 3;
        move.Angle = newAngle;
        genes[idx] = move.Encode();
    }

    /// <summary>
    /// Try to simplify adjacent moves on the same axis/plane.
    /// </summary>
    private static void SimplifyAdjacent(double[] genes, Random rng)
    {
        if (genes.Length < 2) return;

        int idx = rng.Next(genes.Length - 1);
        var move1 = TMove.Decode((int)genes[idx]);
        var move2 = TMove.Decode((int)genes[idx + 1]);

        // If same axis, plane, and slice, combine them
        if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
        {
            int qt1 = move1.Angle + 1;
            int qt2 = move2.Angle + 1;
            int combined = (qt1 + qt2) % 4;

            if (combined == 0)
            {
                // Moves cancel out - replace both with a "neutral" sequence
                // Use the same move with 180° (self-inverse)
                move1.Angle = 1;
                move2.Angle = 1;
            }
            else
            {
                // Combine into one move
                move1.Angle = combined - 1;
                genes[idx] = move1.Encode();
                // Make the second move a 180° (often useful)
                move2.Angle = 1;
            }
            genes[idx] = move1.Encode();
            genes[idx + 1] = move2.Encode();
        }
    }

    /// <summary>
    /// Replace a move with a neighbor (same plane, different slice or angle).
    /// </summary>
    private static void ReplaceWithNeighbor(double[] genes, Random rng)
    {
        int idx = rng.Next(genes.Length);
        var move = TMove.Decode((int)genes[idx]);

        // Modify either slice or angle
        if (rng.NextDouble() < 0.5)
        {
            // Change slice slightly
            int maxSlice = TRubikCube.Size;
            int delta = rng.Next(2) == 0 ? 1 : -1;
            move.Slice = Math.Clamp(move.Slice + delta, 0, maxSlice - 1);
        }
        else
        {
            // Change angle
            move.Angle = (move.Angle + 1) % 3;
        }

        genes[idx] = move.Encode();
    }

    /// <summary>
    /// Try to find and modify cancel-out pairs (R followed by R').
    /// </summary>
    private static void RemoveCancelPair(double[] genes, Random rng, T chromosome)
    {
        if (genes.Length < 2) return;

        // Search for cancel pairs
        for (int i = 0; i < genes.Length - 1; i++)
        {
            var move1 = TMove.Decode((int)genes[i]);
            var move2 = TMove.Decode((int)genes[i + 1]);

            if (move1.Axis == move2.Axis && move1.Plane == move2.Plane &&
                move1.Slice == move2.Slice)
            {
                int qt1 = move1.Angle + 1;
                int qt2 = move2.Angle + 1;

                if ((qt1 + qt2) % 4 == 0)
                {
                    // Found a cancel pair! Replace with random valid moves
                    if (chromosome is IRubikChromosome rubik && rubik.ValidMoves.Count > 0)
                    {
                        genes[i] = rubik.ValidMoves[rng.Next(rubik.ValidMoves.Count)];
                        genes[i + 1] = rubik.ValidMoves[rng.Next(rubik.ValidMoves.Count)];
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Swap a gene with a valid move.
    /// </summary>
    private static void SwapWithValid(double[] genes, Random rng, T chromosome)
    {
        if (chromosome is IRubikChromosome rubik && rubik.ValidMoves.Count > 0)
        {
            int idx = rng.Next(genes.Length);
            genes[idx] = rubik.ValidMoves[rng.Next(rubik.ValidMoves.Count)];
        }
    }

    /// <summary>
    /// Evaluate a chromosome using fitness or heuristics.
    /// </summary>
    private double EvaluateChromosome(T chromosome)
    {
        if (_baseCube != null)
        {
            return EvaluateWithCube(chromosome.Genes);
        }
        return EvaluateHeuristic(chromosome.Genes);
    }

    /// <summary>
    /// Evaluate a candidate modification.
    /// </summary>
    private double EvaluateCandidate(T chromosome, double[] candidate)
    {
        if (_baseCube != null)
        {
            return EvaluateWithCube(candidate);
        }
        return EvaluateHeuristic(candidate);
    }

    /// <summary>
    /// Evaluate using actual cube fitness.
    /// </summary>
    private double EvaluateWithCube(double[] genes)
    {
        var cube = new TRubikCube(_baseCube!);
        double bestFitness = double.MaxValue;

        for (int i = 0; i < genes.Length; i++)
        {
            var move = TMove.Decode((int)genes[i]);
            cube.Turn(move);

            double fitness = cube.Evaluate();
            if (fitness < bestFitness)
            {
                bestFitness = fitness;
            }
        }

        return bestFitness;
    }

    /// <summary>
    /// Evaluate using heuristic scoring (no cube needed).
    /// Lower score = better (fewer redundancies, more coherent).
    /// </summary>
    private static double EvaluateHeuristic(double[] genes)
    {
        double score = 0;

        // Penalize cancel-out pairs
        for (int i = 0; i < genes.Length - 1; i++)
        {
            var move1 = TMove.Decode((int)genes[i]);
            var move2 = TMove.Decode((int)genes[i + 1]);

            if (move1.Axis == move2.Axis && move1.Plane == move2.Plane &&
                move1.Slice == move2.Slice)
            {
                int qt1 = move1.Angle + 1;
                int qt2 = move2.Angle + 1;
                int combined = (qt1 + qt2) % 4;

                if (combined == 0)
                {
                    // Cancel pair - big penalty
                    score += 10;
                }
                else if (combined == qt1 || combined == qt2)
                {
                    // Could be simplified - medium penalty
                    score += 5;
                }
            }
        }

        // Reward move diversity (using different planes/axes)
        var seenPlanes = new HashSet<int>();
        var seenAxes = new HashSet<int>();

        for (int i = 0; i < genes.Length; i++)
        {
            var move = TMove.Decode((int)genes[i]);
            seenPlanes.Add(move.Plane);
            seenAxes.Add(move.Axis);
        }

        // More diversity = better (lower score)
        score -= seenPlanes.Count * 0.5;
        score -= seenAxes.Count * 0.5;

        // Penalize long sequences of same-plane moves
        int sameCount = 1;
        for (int i = 1; i < genes.Length; i++)
        {
            var prev = TMove.Decode((int)genes[i - 1]);
            var curr = TMove.Decode((int)genes[i]);

            if (prev.Plane == curr.Plane)
            {
                sameCount++;
                if (sameCount > 3)
                {
                    score += 2; // Penalty for long same-plane sequences
                }
            }
            else
            {
                sameCount = 1;
            }
        }

        return score;
    }
}
