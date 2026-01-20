using RubikCube.Tests.Mocks;
using TGL.GA.Operators.Mutation;
using Xunit;

namespace RubikCube.Tests.GA.Mutation;

/// <summary>
/// Statistical tests for mutation operators.
/// Verifies distributions, randomness, and statistical properties.
/// </summary>
public class MutationStatisticalTests
{
    private const int TrialCount = 1000;

    #region Position Distribution Tests

    [Fact]
    public void SwapMutation_AllPositionsCanBeSelected()
    {
        var op = new SwapMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never selected for swapping");
        }
    }

    [Fact]
    public void InversionMutation_AllPositionsCanBeInSegment()
    {
        var op = new InversionMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never part of an inversion segment");
        }
    }

    [Fact]
    public void ScrambleMutation_AllPositionsCanBeInSegment()
    {
        var op = new ScrambleMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never part of a scramble segment");
        }
    }

    [Fact]
    public void RandomMutation_AllPositionsCanBeMutated()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 1);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // All positions should be mutated at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > 0,
                $"Position {i} was never selected for random mutation");
        }
    }

    #endregion

    #region Uniformity Tests

    [Fact]
    public void SwapMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new SwapMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        // Expected: ~2000 total changes / 10 positions = ~200 per position
        // Allow for statistical variance (50% tolerance)
        double totalChanges = positionChangeCounts.Sum();
        double expectedPerPosition = totalChanges / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} selected too infrequently: {positionChangeCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    [Fact]
    public void RandomMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 1);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        // Expected: ~1000 total / 10 positions = ~100 per position
        double totalMutations = positionMutationCounts.Sum();
        double expectedPerPosition = totalMutations / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} mutated too infrequently: {positionMutationCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    #endregion


    #region Segment Length Distribution Tests

    [Fact]
    public void InversionMutation_VariousSegmentLengths()
    {
        var op = new InversionMutation<MockChromosome>();
        var segmentLengths = new Dictionary<int, int>();

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find segment length (count of changed positions)
            int changedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            if (changedCount > 0)
            {
                if (!segmentLengths.ContainsKey(changedCount))
                    segmentLengths[changedCount] = 0;
                segmentLengths[changedCount]++;
            }
        }

        // Should produce various segment lengths
        Assert.True(segmentLengths.Count >= 3,
            $"Only {segmentLengths.Count} different segment lengths observed, expected more variety");
    }

    [Fact]
    public void ScrambleMutation_VariousSegmentLengths()
    {
        var op = new ScrambleMutation<MockChromosome>();
        var segmentLengths = new Dictionary<int, int>();

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find segment length (count of changed positions)
            int changedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            if (changedCount > 0)
            {
                if (!segmentLengths.ContainsKey(changedCount))
                    segmentLengths[changedCount] = 0;
                segmentLengths[changedCount]++;
            }
        }

        // Should produce various segment lengths
        Assert.True(segmentLengths.Count >= 3,
            $"Only {segmentLengths.Count} different segment lengths observed, expected more variety");
    }

    #endregion

    #region Mutation Rate Consistency Tests

    [Fact]
    public void RandomMutation_GenesToMutateIsRespected()
    {
        for (int genesToMutate = 1; genesToMutate <= 5; genesToMutate++)
        {
            var op = new RandomMutation<MockChromosome>(genesToMutate: genesToMutate);
            var mutationCounts = new int[11]; // Index = number of mutations

            for (int trial = 0; trial < 500; trial++)
            {
                var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                var original = chromosome.Genes.ToArray();
                var rng = new Random(trial);

                op.Mutate(chromosome, rng);

                int mutations = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                        mutations++;
                }
                mutationCounts[mutations]++;
            }

            // Mutations should be at most genesToMutate
            for (int i = genesToMutate + 1; i < 11; i++)
            {
                Assert.Equal(0, mutationCounts[i]);
            }
        }
    }

    #endregion

    #region GaussianMutation Statistical Tests

    [Fact]
    public void GaussianMutation_AllPositionsCanBeMutated()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 5.0);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // All positions should be mutated at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > 0,
                $"Position {i} was never selected for Gaussian mutation");
        }
    }

    [Fact]
    public void GaussianMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 5.0);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        double totalMutations = positionMutationCounts.Sum();
        double expectedPerPosition = totalMutations / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} mutated too infrequently: {positionMutationCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    [Fact]
    public void GaussianMutation_ChangesAreCenteredAroundZero()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 5, sigma: 10.0);
        double totalChange = 0;
        int changeCount = 0;

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                double diff = chromosome.Genes[i] - original[i];
                if (Math.Abs(diff) > 0.001)
                {
                    totalChange += diff;
                    changeCount++;
                }
            }
        }

        // Mean change should be close to 0 (Gaussian centered at 0)
        double meanChange = totalChange / changeCount;
        Assert.True(Math.Abs(meanChange) < 2.0,
            $"Mean change {meanChange} should be close to 0 for Gaussian distribution");
    }

    #endregion

    #region CreepMutation Statistical Tests

    [Fact]
    public void CreepMutation_AllPositionsCanBeMutated()
    {
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: 5.0);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // All positions should be mutated at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > 0,
                $"Position {i} was never selected for creep mutation");
        }
    }

    [Fact]
    public void CreepMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: 5.0);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        double totalMutations = positionMutationCounts.Sum();
        double expectedPerPosition = totalMutations / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} mutated too infrequently: {positionMutationCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    [Fact]
    public void CreepMutation_SingleMutationIsWithinCreepRange()
    {
        // Use genesToMutate=1 to test individual mutation is within range
        double creepRange = 5.0;
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: creepRange);

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                double diff = Math.Abs(chromosome.Genes[i] - original[i]);
                if (diff > 0.001)
                {
                    Assert.True(diff <= creepRange + 0.001,
                        $"Change {diff} exceeds creep range {creepRange}");
                }
            }
        }
    }

    #endregion

    #region DisplacementMutation Statistical Tests

    [Fact]
    public void DisplacementMutation_AllPositionsCanBeDisplaced()
    {
        var op = new DisplacementMutation<MockChromosome>(minSegmentSize: 2, maxSegmentSize: 4);
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never part of a displacement");
        }
    }

    [Fact]
    public void DisplacementMutation_VariousSegmentLengths()
    {
        var op = new DisplacementMutation<MockChromosome>(minSegmentSize: 2, maxSegmentSize: 5);
        var segmentLengths = new Dictionary<int, int>();

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            int changedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            if (changedCount > 0)
            {
                if (!segmentLengths.ContainsKey(changedCount))
                    segmentLengths[changedCount] = 0;
                segmentLengths[changedCount]++;
            }
        }

        // Should produce various segment lengths
        Assert.True(segmentLengths.Count >= 2,
            $"Only {segmentLengths.Count} different segment lengths observed, expected more variety");
    }

    #endregion

    #region ShiftMutation Statistical Tests

    [Fact]
    public void ShiftMutation_AllPositionsCanBeAffected()
    {
        var op = new ShiftMutation<MockChromosome>(segmentOnly: false);
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never affected by shift mutation");
        }
    }

    [Fact]
    public void ShiftMutation_PreservesAllGeneValues()
    {
        var op = new ShiftMutation<MockChromosome>(segmentOnly: false);

        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            var newSorted = chromosome.Genes.OrderBy(x => x).ToArray();

            // Same values should be present (shift is a permutation)
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(originalSorted[i], newSorted[i]);
            }
        }
    }

    #endregion

    #region TranslocationMutation Statistical Tests

    [Fact]
    public void TranslocationMutation_AllPositionsCanBeAffected()
    {
        var op = new TranslocationMutation<MockChromosome>(minSegmentSize: 2, maxSegmentSize: 4);
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never affected by translocation mutation");
        }
    }

    [Fact]
    public void TranslocationMutation_PreservesAllGeneValues()
    {
        var op = new TranslocationMutation<MockChromosome>(minSegmentSize: 2, maxSegmentSize: 4);

        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            var newSorted = chromosome.Genes.OrderBy(x => x).ToArray();

            // Same values should be present (translocation is a permutation)
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(originalSorted[i], newSorted[i]);
            }
        }
    }

    #endregion

    #region InsertMutation Statistical Tests

    [Fact]
    public void InsertMutation_AllPositionsCanBeSelected()
    {
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var op = new InsertMutation<MockRubikChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < 10; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // Most positions should be affected at least sometimes
        int affectedPositions = positionChangeCounts.Count(c => c > 0);
        Assert.True(affectedPositions >= 5,
            $"Only {affectedPositions} positions were ever affected by insert mutation");
    }

    #endregion

    #region SingleGeneMutation Statistical Tests

    [Fact]
    public void SingleGeneMutation_AllPositionsCanBeMutated()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var positionMutationCounts = new int[global::GA.TChromosome.GenesLength];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            // Set genes to sentinel values
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = -1 - i;

            var original = genome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(genome, rng);

            for (int i = 0; i < genome.Length; i++)
            {
                if (Math.Abs(original[i] - genome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // All positions should be mutated at least sometimes
        for (int i = 0; i < positionMutationCounts.Length; i++)
        {
            Assert.True(positionMutationCounts[i] > 0,
                $"Position {i} was never selected for single gene mutation");
        }
    }

    [Fact]
    public void SingleGeneMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var positionMutationCounts = new int[global::GA.TChromosome.GenesLength];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = -1 - i;

            var original = genome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(genome, rng);

            for (int i = 0; i < genome.Length; i++)
            {
                if (Math.Abs(original[i] - genome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        double totalMutations = positionMutationCounts.Sum();
        double expectedPerPosition = totalMutations / positionMutationCounts.Length;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < positionMutationCounts.Length; i++)
        {
            Assert.True(positionMutationCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} mutated too infrequently: {positionMutationCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    #endregion

    #region AdaptiveMutation Statistical Tests

    [Fact]
    public void AdaptiveMutation_HighFitnessMutatesMoreGenes()
    {
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var op = new AdaptiveMutation<MockRubikChromosome>(
            minGenes: 1, maxGenes: 5,
            minExpectedFitness: 0, maxExpectedFitness: 100);

        var highFitnessChanges = new List<int>();
        var lowFitnessChanges = new List<int>();

        for (int trial = 0; trial < 500; trial++)
        {
            // High fitness (bad) chromosome
            var highFitChrom = new MockRubikChromosome(10);
            highFitChrom.ValidMoves = validMoves;
            highFitChrom.Fitness = 90;
            for (int i = 0; i < 10; i++)
                highFitChrom.Genes[i] = validMoves[0];

            var origHigh = highFitChrom.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(highFitChrom, rng);

            int changesHigh = 0;
            for (int i = 0; i < 10; i++)
                if (Math.Abs(origHigh[i] - highFitChrom.Genes[i]) > 0.001)
                    changesHigh++;
            highFitnessChanges.Add(changesHigh);

            // Low fitness (good) chromosome
            var lowFitChrom = new MockRubikChromosome(10);
            lowFitChrom.ValidMoves = validMoves;
            lowFitChrom.Fitness = 10;
            for (int i = 0; i < 10; i++)
                lowFitChrom.Genes[i] = validMoves[0];

            var origLow = lowFitChrom.Genes.ToArray();
            rng = new Random(trial);
            op.Mutate(lowFitChrom, rng);

            int changesLow = 0;
            for (int i = 0; i < 10; i++)
                if (Math.Abs(origLow[i] - lowFitChrom.Genes[i]) > 0.001)
                    changesLow++;
            lowFitnessChanges.Add(changesLow);
        }

        double avgHigh = highFitnessChanges.Average();
        double avgLow = lowFitnessChanges.Average();

        Assert.True(avgHigh > avgLow,
            $"High fitness avg changes ({avgHigh}) should be greater than low fitness ({avgLow})");
    }

    #endregion
}
