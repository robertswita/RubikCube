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
///
/// Basic modifications:
/// - Changing the angle of a single move
/// - Replacing a move with a similar one
/// - Removing redundant move pairs
/// - Simplifying consecutive moves
/// - Swap with a valid move
///
/// Advanced modifications:
/// - Move insertion: Insert a new move at a random position
/// - Move deletion: Remove a move (shift others to fill gap)
/// - Non-adjacent swap: Swap two moves that are not adjacent
/// - Pattern-based modifications: Apply known simplification patterns
///   - Commutator detection and simplification
///   - Conjugate pattern optimization
///   - Trigger sequence recognition
///   - Redundant sequence removal (R R' -> nothing)
/// - Gradient-based angle optimization: Systematically try all angles
///   for a position and keep the best
///
/// The "best" modification is selected using a heuristic score that considers:
/// - Cancellation opportunities (moves that can be simplified)
/// - Sequence coherence (similar moves grouped together)
/// - Redundancy (fewer moves that cancel out)
/// - Pattern recognition (bonus for known useful patterns)
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

    // Known patterns for pattern-based modifications
    private static readonly List<(int[] pattern, int[] replacement, string name)> _simplificationPatterns = new();
    private static bool _patternsInitialized = false;
    private static int _patternCacheN = -1;
    private static int _patternCacheSize = -1;

    /// <summary>
    /// Creates a local search mutation with default parameters.
    /// </summary>
    public LocalSearchMutation() : this(8, 4) { }

    /// <summary>
    /// Creates a local search mutation with specified parameters.
    /// </summary>
    /// <param name="neighborhoodSize">Number of candidate modifications to try per iteration.</param>
    /// <param name="maxIterations">Maximum iterations of local search.</param>
    public LocalSearchMutation(int neighborhoodSize = 8, int maxIterations = 4)
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

        // Initialize patterns if needed
        InitializePatternsIfNeeded();

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
    /// Initialize simplification patterns for pattern-based modifications.
    /// </summary>
    private static void InitializePatternsIfNeeded()
    {
        int n = TAffine.N;
        int size = TRubikCube.Size;

        if (_patternsInitialized && _patternCacheN == n && _patternCacheSize == size)
            return;

        _simplificationPatterns.Clear();

        if (n == 3)
        {
            int s = size - 1;

            // Encode basic moves
            int R = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 0 }.Encode();
            int Ri = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 2 }.Encode();
            int R2 = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 1 }.Encode();

            int U = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 0 }.Encode();
            int Ui = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 2 }.Encode();
            int U2 = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 1 }.Encode();

            int F = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 0 }.Encode();
            int Fi = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 2 }.Encode();
            int F2 = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 1 }.Encode();

            int L = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 2 }.Encode();
            int Li = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 0 }.Encode();
            int L2 = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 1 }.Encode();

            // Pattern: Double sexy = (R U R' U')2 can sometimes be simplified
            // Note: These are patterns to TRY, not guaranteed simplifications

            // Commutator-like patterns (ABA'B' patterns that might be redundant)
            _simplificationPatterns.Add((
                new[] { R, U, Ri, Ui, R, U, Ri, Ui },
                new[] { R, U, Ri, Ui }, // Try replacing double sexy with single
                "Double sexy -> Single"
            ));

            // Four-move trigger patterns
            _simplificationPatterns.Add((
                new[] { R, U, Ri, U },
                new[] { R, U2, Ri }, // Alternative form
                "R U R' U -> R U2 R' (alt)"
            ));

            _simplificationPatterns.Add((
                new[] { U, R, Ui, R },
                new[] { R2, U }, // Possible simplification
                "U R U' R -> R2 U (alt)"
            ));

            // Conjugate patterns: A X A' patterns
            _simplificationPatterns.Add((
                new[] { R, U2, Ri },
                new[] { Ri, U2, R }, // Equivalent
                "R U2 R' <-> R' U2 R"
            ));

            // Sune variants
            _simplificationPatterns.Add((
                new[] { R, U, Ri, U, R, U, Ri },
                new[] { R, U, Ri, U, R, Ui, Ri }, // Different ending
                "Sune variant"
            ));
        }

        _patternsInitialized = true;
        _patternCacheN = n;
        _patternCacheSize = size;
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

            // Choose a random modification type (expanded set)
            int modType = rng.Next(11);

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

                case 5:
                    // NEW: Move insertion trial
                    TryMoveInsertion(candidate, rng, chromosome);
                    break;

                case 6:
                    // NEW: Move deletion trial
                    TryMoveDeletion(candidate, rng, chromosome);
                    break;

                case 7:
                    // NEW: Non-adjacent swap
                    TryNonAdjacentSwap(candidate, rng);
                    break;

                case 8:
                    // NEW: Pattern-based modification
                    TryPatternModification(candidate, rng);
                    break;

                case 9:
                    // NEW: Gradient-based angle optimization
                    TryGradientAngleOptimization(candidate, rng, chromosome);
                    break;

                case 10:
                    // NEW: Multi-position gradient optimization
                    TryMultiPositionGradient(candidate, rng, chromosome);
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

    // ============================================================
    // NEW MODIFICATION TYPES
    // ============================================================

    /// <summary>
    /// Try inserting a move at a random position.
    /// Since chromosome length is fixed, we shift and lose the last element.
    /// </summary>
    private static void TryMoveInsertion(double[] genes, Random rng, T chromosome)
    {
        if (genes.Length < 3) return;

        int insertPos = rng.Next(genes.Length - 1);

        // Get a new move to insert
        int newMove;
        if (chromosome is IRubikChromosome rubik && rubik.ValidMoves.Count > 0)
        {
            newMove = rubik.ValidMoves[rng.Next(rubik.ValidMoves.Count)];
        }
        else
        {
            return;
        }

        // Shift elements right from insertPos (losing the last one)
        for (int i = genes.Length - 1; i > insertPos; i--)
        {
            genes[i] = genes[i - 1];
        }

        // Insert the new move
        genes[insertPos] = newMove;
    }

    /// <summary>
    /// Try deleting a move at a random position.
    /// Since chromosome length is fixed, we shift left and add a new move at the end.
    /// </summary>
    private static void TryMoveDeletion(double[] genes, Random rng, T chromosome)
    {
        if (genes.Length < 3) return;

        int deletePos = rng.Next(genes.Length - 1);

        // Shift elements left from deletePos
        for (int i = deletePos; i < genes.Length - 1; i++)
        {
            genes[i] = genes[i + 1];
        }

        // Add a new move at the end (or copy a move from the sequence)
        if (chromosome is IRubikChromosome rubik && rubik.ValidMoves.Count > 0)
        {
            // Either add a neutral move (180°) or a random valid move
            if (rng.NextDouble() < 0.3)
            {
                // Copy a move from elsewhere in the sequence for coherence
                genes[genes.Length - 1] = genes[rng.Next(genes.Length - 1)];
            }
            else
            {
                genes[genes.Length - 1] = rubik.ValidMoves[rng.Next(rubik.ValidMoves.Count)];
            }
        }
    }

    /// <summary>
    /// Try swapping two non-adjacent moves.
    /// </summary>
    private static void TryNonAdjacentSwap(double[] genes, Random rng)
    {
        if (genes.Length < 4) return;

        // Select two positions with at least 2 positions apart
        int pos1 = rng.Next(genes.Length - 3);
        int pos2 = pos1 + 2 + rng.Next(genes.Length - pos1 - 2);

        // Swap the moves
        (genes[pos1], genes[pos2]) = (genes[pos2], genes[pos1]);
    }

    /// <summary>
    /// Try applying a known pattern modification.
    /// Searches for patterns in the sequence and replaces with alternatives.
    /// </summary>
    private static void TryPatternModification(double[] genes, Random rng)
    {
        if (_simplificationPatterns.Count == 0) return;

        // Try a random pattern
        var (pattern, replacement, _) = _simplificationPatterns[rng.Next(_simplificationPatterns.Count)];

        // Search for the pattern in genes
        for (int i = 0; i <= genes.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length && match; j++)
            {
                if ((int)genes[i + j] != pattern[j])
                {
                    match = false;
                }
            }

            if (match)
            {
                // Found the pattern! Apply replacement
                int replaceLen = Math.Min(replacement.Length, pattern.Length);
                for (int j = 0; j < replaceLen; j++)
                {
                    genes[i + j] = replacement[j];
                }

                // If replacement is shorter, fill remaining with copies or shifts
                for (int j = replaceLen; j < pattern.Length; j++)
                {
                    // Copy adjacent moves to fill the gap
                    if (i + j + 1 < genes.Length)
                    {
                        genes[i + j] = genes[i + j + 1];
                    }
                }
                return;
            }
        }

        // No pattern found - try a generic simplification
        TryGenericPatternSimplification(genes, rng);
    }

    /// <summary>
    /// Try generic pattern simplifications that work for any dimension.
    /// </summary>
    private static void TryGenericPatternSimplification(double[] genes, Random rng)
    {
        // Look for X X patterns (same move twice) -> X2 or shift
        for (int i = 0; i < genes.Length - 1; i++)
        {
            var move1 = TMove.Decode((int)genes[i]);
            var move2 = TMove.Decode((int)genes[i + 1]);

            // Same move twice with angle 0 (90°) -> becomes 180°
            if (move1.Axis == move2.Axis && move1.Plane == move2.Plane &&
                move1.Slice == move2.Slice && move1.Angle == move2.Angle)
            {
                if (move1.Angle == 0) // Both are 90° CW
                {
                    // R R -> R2
                    move1.Angle = 1; // 180°
                    genes[i] = move1.Encode();

                    // Shift the rest left
                    for (int j = i + 1; j < genes.Length - 1; j++)
                    {
                        genes[j] = genes[j + 1];
                    }
                    // Fill last position
                    if (i + 2 < genes.Length)
                    {
                        var lastMove = TMove.Decode((int)genes[genes.Length - 2]);
                        lastMove.Angle = (lastMove.Angle + 1) % 3; // Vary it
                        genes[genes.Length - 1] = lastMove.Encode();
                    }
                    return;
                }
                else if (move1.Angle == 2) // Both are 90° CCW
                {
                    // R' R' -> R2
                    move1.Angle = 1; // 180°
                    genes[i] = move1.Encode();

                    for (int j = i + 1; j < genes.Length - 1; j++)
                    {
                        genes[j] = genes[j + 1];
                    }
                    return;
                }
            }

            // X X' pattern (inverse) -> try removing both
            if (move1.Axis == move2.Axis && move1.Plane == move2.Plane &&
                move1.Slice == move2.Slice)
            {
                int qt1 = move1.Angle + 1;
                int qt2 = move2.Angle + 1;
                if ((qt1 + qt2) % 4 == 0)
                {
                    // They cancel! Shift everything left by 2
                    for (int j = i; j < genes.Length - 2; j++)
                    {
                        genes[j] = genes[j + 2];
                    }
                    // Fill last two positions with varied moves
                    var baseMove = TMove.Decode((int)genes[Math.Max(0, i - 1)]);
                    baseMove.Angle = 1; // 180° is neutral-ish
                    genes[genes.Length - 2] = baseMove.Encode();
                    baseMove.Angle = 0;
                    genes[genes.Length - 1] = baseMove.Encode();
                    return;
                }
            }
        }

        // Look for X Y X' patterns (conjugate with immediate inverse)
        for (int i = 0; i < genes.Length - 2; i++)
        {
            var move1 = TMove.Decode((int)genes[i]);
            var move3 = TMove.Decode((int)genes[i + 2]);

            if (move1.Axis == move3.Axis && move1.Plane == move3.Plane &&
                move1.Slice == move3.Slice)
            {
                int qt1 = move1.Angle + 1;
                int qt3 = move3.Angle + 1;
                if ((qt1 + qt3) % 4 == 0)
                {
                    // X Y X' pattern - the X and X' can potentially be simplified
                    // Try replacing with just Y or Y with different setup
                    var moveY = TMove.Decode((int)genes[i + 1]);
                    genes[i] = genes[i + 1]; // Move Y to position of X
                    moveY.Angle = (moveY.Angle + 1) % 3;
                    genes[i + 1] = moveY.Encode();
                    // Shift rest
                    for (int j = i + 2; j < genes.Length - 1; j++)
                    {
                        genes[j] = genes[j + 1];
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Gradient-based angle optimization: try all 3 angles for a random position.
    /// Keep the angle that gives the best heuristic score.
    /// </summary>
    private void TryGradientAngleOptimization(double[] genes, Random rng, T chromosome)
    {
        int idx = rng.Next(genes.Length);
        var originalMove = TMove.Decode((int)genes[idx]);

        double bestScore = double.MaxValue;
        int bestAngle = originalMove.Angle;

        // Try all 3 angles
        for (int angle = 0; angle < 3; angle++)
        {
            var testMove = new TMove
            {
                Axis = originalMove.Axis,
                Slice = originalMove.Slice,
                Plane = originalMove.Plane,
                Angle = angle
            };
            genes[idx] = testMove.Encode();

            double score = _baseCube != null ? EvaluateWithCube(genes) : EvaluateHeuristic(genes);

            if (score < bestScore)
            {
                bestScore = score;
                bestAngle = angle;
            }
        }

        // Set the best angle found
        originalMove.Angle = bestAngle;
        genes[idx] = originalMove.Encode();
    }

    /// <summary>
    /// Multi-position gradient optimization: optimize angles for multiple positions.
    /// </summary>
    private void TryMultiPositionGradient(double[] genes, Random rng, T chromosome)
    {
        // Select 2-3 positions to optimize
        int numPositions = Math.Min(3, genes.Length);
        var positions = new HashSet<int>();

        while (positions.Count < numPositions)
        {
            positions.Add(rng.Next(genes.Length));
        }

        foreach (int idx in positions)
        {
            var originalMove = TMove.Decode((int)genes[idx]);
            double bestScore = double.MaxValue;
            int bestAngle = originalMove.Angle;

            // Try all 3 angles
            for (int angle = 0; angle < 3; angle++)
            {
                var testMove = new TMove
                {
                    Axis = originalMove.Axis,
                    Slice = originalMove.Slice,
                    Plane = originalMove.Plane,
                    Angle = angle
                };
                genes[idx] = testMove.Encode();

                double score = _baseCube != null ? EvaluateWithCube(genes) : EvaluateHeuristic(genes);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestAngle = angle;
                }
            }

            // Set the best angle found for this position
            originalMove.Angle = bestAngle;
            genes[idx] = originalMove.Encode();
        }
    }

    // ============================================================
    // EVALUATION METHODS
    // ============================================================

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
    /// Enhanced with pattern recognition bonuses.
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

        // Check for X Y X' patterns (redundant conjugates)
        for (int i = 0; i < genes.Length - 2; i++)
        {
            var move1 = TMove.Decode((int)genes[i]);
            var move3 = TMove.Decode((int)genes[i + 2]);

            if (move1.Axis == move3.Axis && move1.Plane == move3.Plane &&
                move1.Slice == move3.Slice)
            {
                int qt1 = move1.Angle + 1;
                int qt3 = move3.Angle + 1;
                if ((qt1 + qt3) % 4 == 0)
                {
                    // Potential redundant conjugate
                    score += 3;
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

        // Bonus for useful trigger patterns (sexy move, sledgehammer)
        // Check for R U R' U' pattern (sexy move)
        for (int i = 0; i < genes.Length - 3; i++)
        {
            var m1 = TMove.Decode((int)genes[i]);
            var m2 = TMove.Decode((int)genes[i + 1]);
            var m3 = TMove.Decode((int)genes[i + 2]);
            var m4 = TMove.Decode((int)genes[i + 3]);

            // Check for A B A' B' commutator structure
            if (m1.Axis == m3.Axis && m1.Plane == m3.Plane && m1.Slice == m3.Slice &&
                m2.Axis == m4.Axis && m2.Plane == m4.Plane && m2.Slice == m4.Slice)
            {
                int qt1 = m1.Angle + 1;
                int qt3 = m3.Angle + 1;
                int qt2 = m2.Angle + 1;
                int qt4 = m4.Angle + 1;

                if ((qt1 + qt3) % 4 == 0 && (qt2 + qt4) % 4 == 0)
                {
                    // Found a commutator pattern - this is generally good!
                    score -= 2;
                }
            }
        }

        // Bonus for having 180° moves (often useful for efficient solving)
        int count180 = 0;
        for (int i = 0; i < genes.Length; i++)
        {
            var move = TMove.Decode((int)genes[i]);
            if (move.Angle == 1) count180++;
        }
        score -= count180 * 0.2; // Small bonus for 180° moves

        return score;
    }
}
