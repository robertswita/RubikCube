using RubikCube.Tests.Mocks;
using TGL.GA.Operators.Mutation;
using Xunit;
using MockTChromosome = global::GA.TChromosome;

namespace RubikCube.Tests.GA.Mutation;

/// <summary>
/// Operator-specific behavior tests for mutation operators.
/// Verifies that each operator performs its documented mutation strategy.
/// </summary>
public class MutationOperatorSpecificTests
{
    #region SingleGeneMutation Tests

    [Fact]
    public void SingleGeneMutation_DoesNotThrow()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var genome = new RubikCube.TRubikGenome();
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(genome, rng));
        Assert.Null(exception);
    }

    [Fact]
    public void SingleGeneMutation_ChangesExactlyOneGene()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();

        int singleChangeCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            var original = genome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(genome, rng);

            int differences = 0;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - genome.Genes[i]) > 0.001)
                    differences++;
            }

            // Should change exactly 0 or 1 gene (0 if new value happens to equal old)
            Assert.True(differences <= 1, $"Changed {differences} genes, expected at most 1");
            if (differences == 1)
                singleChangeCount++;
        }

        Assert.True(singleChangeCount > 0, "SingleGeneMutation never changed a gene in 100 trials");
    }

    [Fact]
    public void SingleGeneMutation_UsesValidMovesFromFreeMoves()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var validMoves = new HashSet<int>(RubikCube.TRubikGenome.FreeMoves);

        for (int trial = 0; trial < 100; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            // Set genes to values NOT in FreeMoves to detect changes
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = -1;

            var rng = new Random(trial);
            op.Mutate(genome, rng);

            // Find which gene was changed
            for (int i = 0; i < genome.Length; i++)
            {
                if (genome.Genes[i] != -1)
                {
                    // The new value should be in FreeMoves
                    Assert.True(validMoves.Contains((int)genome.Genes[i]),
                        $"Gene value {genome.Genes[i]} is not a valid move from FreeMoves");
                }
            }
        }
    }

    [Fact]
    public void SingleGeneMutation_AllPositionsCanBeSelected()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var positionChangeCounts = new int[MockTChromosome.GenesLength];

        for (int trial = 0; trial < 1000; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            // Set genes to sentinel values
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = -1 - i; // Unique negative values

            var rng = new Random(trial);
            op.Mutate(genome, rng);

            for (int i = 0; i < genome.Length; i++)
            {
                if (genome.Genes[i] != -1 - i)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < positionChangeCounts.Length; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never selected for mutation");
        }
    }

    [Fact]
    public void SingleGeneMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var positionChangeCounts = new int[MockTChromosome.GenesLength];

        for (int trial = 0; trial < 1000; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = -1 - i;

            var rng = new Random(trial);
            op.Mutate(genome, rng);

            for (int i = 0; i < genome.Length; i++)
            {
                if (genome.Genes[i] != -1 - i)
                    positionChangeCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        // Expected: ~1000 / 10 = ~100 per position, allow 50% tolerance
        double expectedPerPosition = 1000.0 / MockTChromosome.GenesLength;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < positionChangeCounts.Length; i++)
        {
            Assert.True(positionChangeCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} selected too infrequently: {positionChangeCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    [Fact]
    public void SingleGeneMutation_SameSeedSameResult()
    {
        var op1 = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var op2 = new SingleGeneMutation<RubikCube.TRubikGenome>();

        var genome1 = new RubikCube.TRubikGenome();
        var genome2 = new RubikCube.TRubikGenome();

        // Ensure same initial state
        for (int i = 0; i < genome1.Length; i++)
        {
            genome1.Genes[i] = i;
            genome2.Genes[i] = i;
        }

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        op1.Mutate(genome1, rng1);
        op2.Mutate(genome2, rng2);

        Assert.Equal(genome1.Genes, genome2.Genes);
    }

    [Fact]
    public void SingleGeneMutation_DifferentSeedsDifferentResults()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();

        var results = new List<double[]>();
        for (int seed = 0; seed < 20; seed++)
        {
            var genome = new RubikCube.TRubikGenome();
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = i;

            var rng = new Random(seed);
            op.Mutate(genome, rng);
            results.Add(genome.Genes.ToArray());
        }

        int uniqueCount = results.Select(r => string.Join(",", r)).Distinct().Count();
        Assert.True(uniqueCount > 1, "All results were identical with different seeds");
    }

    [Fact]
    public void SingleGeneMutation_IgnoresNonTRubikGenome()
    {
        var op = new SingleGeneMutation<MockChromosome>();
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var original = chromosome.Genes.ToArray();
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // Should not change anything since MockChromosome is not TRubikGenome
        Assert.Equal(original, chromosome.Genes);
    }

    [Fact]
    public void SingleGeneMutation_PreservesOtherGenes()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();

        for (int trial = 0; trial < 50; trial++)
        {
            var genome = new RubikCube.TRubikGenome();
            // Set distinct values
            for (int i = 0; i < genome.Length; i++)
                genome.Genes[i] = i * 100;

            var original = genome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(genome, rng);

            // Find which gene changed
            int changedIndex = -1;
            for (int i = 0; i < genome.Length; i++)
            {
                if (Math.Abs(original[i] - genome.Genes[i]) > 0.001)
                {
                    changedIndex = i;
                    break;
                }
            }

            // Verify all other genes are unchanged
            for (int i = 0; i < genome.Length; i++)
            {
                if (i != changedIndex)
                {
                    Assert.Equal(original[i], genome.Genes[i], 3);
                }
            }
        }
    }

    [Fact]
    public void SingleGeneMutation_MultipleMutationsDoNotCorruptState()
    {
        var op = new SingleGeneMutation<RubikCube.TRubikGenome>();
        var genome = new RubikCube.TRubikGenome();
        var rng = new Random(42);
        var validMoves = new HashSet<int>(RubikCube.TRubikGenome.FreeMoves);

        // Perform many mutations
        for (int i = 0; i < 100; i++)
        {
            op.Mutate(genome, rng);

            // Verify genome is still valid
            Assert.Equal(MockTChromosome.GenesLength, genome.Length);
            Assert.NotNull(genome.Genes);
        }
    }

    [Fact]
    public void SingleGeneMutation_FreeMovesArePopulated()
    {
        // Verify that FreeMoves contains valid move codes
        Assert.True(RubikCube.TRubikGenome.FreeMoves.Count > 0,
            "FreeMoves should be populated with valid moves");

        // For 3D cube (N=3) with Size=3 and 3 planes, total moves = 3 * 3 * 3 * 3 = 81
        // All should be valid
        int expectedMoves = TGL.TAffine.N * RubikCube.TRubikCube.Size * TGL.TAffine.Planes.Length * 3;
        Assert.Equal(expectedMoves, RubikCube.TRubikGenome.FreeMoves.Count);
    }

    #endregion

    #region SwapMutation Tests

    [Fact]
    public void SwapMutation_SwapsExactlyTwoGenes()
    {
        var op = new SwapMutation<MockChromosome>();

        int twoSwapCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Count differences
            int differences = 0;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    differences++;
            }

            // Swap should change exactly 0 or 2 positions
            if (differences == 2)
                twoSwapCount++;
            else
                Assert.True(differences == 0, $"Swap changed {differences} positions, expected 0 or 2");
        }

        Assert.True(twoSwapCount > 0, "SwapMutation never swapped two genes in 100 trials");
    }

    [Fact]
    public void SwapMutation_SwappedValuesAreExchanged()
    {
        var op = new SwapMutation<MockChromosome>();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find changed positions
            var changedIndices = new List<int>();
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedIndices.Add(i);
            }

            if (changedIndices.Count == 2)
            {
                int i = changedIndices[0];
                int j = changedIndices[1];
                // Values should be exchanged
                Assert.Equal(original[i], chromosome.Genes[j], 3);
                Assert.Equal(original[j], chromosome.Genes[i], 3);
            }
        }
    }

    #endregion

    #region InversionMutation Tests

    [Fact]
    public void InversionMutation_ReversesASegment()
    {
        var op = new InversionMutation<MockChromosome>();

        int reversalCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find the segment that was reversed
            int start = -1, end = -1;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    if (start == -1) start = i;
                    end = i;
                }
            }

            if (start != -1 && start != end)
            {
                // Verify the segment is reversed
                bool isReversed = true;
                for (int i = start; i <= end; i++)
                {
                    if (Math.Abs(original[start + (end - i)] - chromosome.Genes[i]) > 0.001)
                    {
                        isReversed = false;
                        break;
                    }
                }

                if (isReversed)
                    reversalCount++;
            }
        }

        Assert.True(reversalCount > 0, "InversionMutation never performed a reversal in 100 trials");
    }

    [Fact]
    public void InversionMutation_PreservesGeneValues()
    {
        var op = new InversionMutation<MockChromosome>();
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();
        var rng = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            op.Mutate(chromosome, rng);
            var currentSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            Assert.Equal(originalSorted, currentSorted);
        }
    }

    #endregion

    #region ScrambleMutation Tests

    [Fact]
    public void ScrambleMutation_ShufflesASegment()
    {
        var op = new ScrambleMutation<MockChromosome>();

        int scrambleCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find unchanged prefix and suffix
            int start = 0;
            while (start < original.Length && Math.Abs(original[start] - chromosome.Genes[start]) < 0.001)
                start++;

            int end = original.Length - 1;
            while (end >= 0 && Math.Abs(original[end] - chromosome.Genes[end]) < 0.001)
                end--;

            if (start <= end)
            {
                // Verify scrambled segment has same values (multiset)
                var originalSegment = original.Skip(start).Take(end - start + 1).OrderBy(x => x);
                var scrambledSegment = chromosome.Genes.Skip(start).Take(end - start + 1).OrderBy(x => x);
                Assert.Equal(originalSegment, scrambledSegment);
                scrambleCount++;
            }
        }

        Assert.True(scrambleCount > 0, "ScrambleMutation never scrambled in 100 trials");
    }

    [Fact]
    public void ScrambleMutation_PreservesGeneMultiset()
    {
        var op = new ScrambleMutation<MockChromosome>();
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();
        var rng = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            op.Mutate(chromosome, rng);
            var currentSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            Assert.Equal(originalSorted, currentSorted);
        }
    }

    #endregion

    #region ShiftMutation Tests

    [Fact]
    public void ShiftMutation_PerformsCircularRotation()
    {
        var op = new ShiftMutation<MockChromosome>(segmentOnly: false);

        int rotationCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            if (!original.SequenceEqual(chromosome.Genes))
            {
                // Verify it's a rotation - all elements should still be present
                var originalSet = new HashSet<double>(original);
                var resultSet = new HashSet<double>(chromosome.Genes);
                Assert.Equal(originalSet, resultSet);
                rotationCount++;
            }
        }

        Assert.True(rotationCount > 0, "ShiftMutation never rotated in 100 trials");
    }

    [Fact]
    public void ShiftMutation_SegmentOnly_ShiftsSegment()
    {
        var op = new ShiftMutation<MockChromosome>(segmentOnly: true);

        int shiftCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            if (!original.SequenceEqual(chromosome.Genes))
            {
                // Gene set should be preserved
                var originalSet = new HashSet<double>(original);
                var resultSet = new HashSet<double>(chromosome.Genes);
                Assert.Equal(originalSet, resultSet);
                shiftCount++;
            }
        }

        Assert.True(shiftCount > 0, "ShiftMutation with segmentOnly never shifted in 100 trials");
    }

    #endregion

    #region DisplacementMutation Tests

    [Fact]
    public void DisplacementMutation_MovesSegmentToNewPosition()
    {
        var op = new DisplacementMutation<MockChromosome>();

        int displacementCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            if (!original.SequenceEqual(chromosome.Genes))
            {
                // Verify gene multiset is preserved
                var originalSorted = original.OrderBy(x => x).ToArray();
                var resultSorted = chromosome.Genes.OrderBy(x => x).ToArray();
                Assert.Equal(originalSorted, resultSorted);
                displacementCount++;
            }
        }

        Assert.True(displacementCount > 0, "DisplacementMutation never displaced in 100 trials");
    }

    [Fact]
    public void DisplacementMutation_PreservesAllGenes()
    {
        var op = new DisplacementMutation<MockChromosome>();
        var rng = new Random(42);

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();

            op.Mutate(chromosome, new Random(trial));

            var resultSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            Assert.Equal(originalSorted, resultSorted);
        }
    }

    #endregion

    #region TranslocationMutation Tests

    [Fact]
    public void TranslocationMutation_SwapsTwoSegments()
    {
        var op = new TranslocationMutation<MockChromosome>();

        int translocationCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            if (!original.SequenceEqual(chromosome.Genes))
            {
                // Verify gene multiset is preserved
                var originalSorted = original.OrderBy(x => x).ToArray();
                var resultSorted = chromosome.Genes.OrderBy(x => x).ToArray();
                Assert.Equal(originalSorted, resultSorted);
                translocationCount++;
            }
        }

        Assert.True(translocationCount > 0, "TranslocationMutation never translocated in 100 trials");
    }

    [Fact]
    public void TranslocationMutation_PreservesAllGenes()
    {
        var op = new TranslocationMutation<MockChromosome>();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();

            op.Mutate(chromosome, new Random(trial));

            var resultSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            Assert.Equal(originalSorted, resultSorted);
        }
    }

    #endregion

    #region RandomMutation Tests

    [Fact]
    public void RandomMutation_ReplacesGeneWithRandomValue()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 1);

        int mutationCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            int differences = 0;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    differences++;
            }

            if (differences > 0)
                mutationCount++;
        }

        Assert.True(mutationCount > 0, "RandomMutation never mutated in 100 trials");
    }

    [Fact]
    public void RandomMutation_MutatesRequestedNumberOfGenes()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 3);

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            int differences = 0;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    differences++;
            }

            // Should mutate at most genesToMutate genes
            Assert.True(differences <= 3, $"RandomMutation changed {differences} genes, expected at most 3");
        }
    }

    [Fact]
    public void RandomMutation_DefaultMutatesOneGene()
    {
        var op = new RandomMutation<MockChromosome>(); // Default is 1

        int singleMutationCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            int differences = 0;
            for (int i = 0; i < original.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    differences++;
            }

            if (differences == 1)
                singleMutationCount++;
        }

        Assert.True(singleMutationCount > 0, "RandomMutation with default never changed exactly one gene");
    }

    #endregion

}
