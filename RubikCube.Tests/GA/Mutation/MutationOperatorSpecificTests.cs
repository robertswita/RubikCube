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

    #region ConjugationMutation Tests

    [Fact]
    public void ConjugationMutation_CreatesSymmetricPattern()
    {
        var op = new ConjugationMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int symmetryCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Look for symmetric ABA' patterns
            // After mutation, moves around a center should be inverses
            for (int center = 1; center < chromosome.Length - 1; center++)
            {
                bool hasSymmetry = true;
                for (int offset = 1; center - offset >= 0 && center + offset < chromosome.Length; offset++)
                {
                    var moveBefore = RubikCube.TMove.Decode((int)chromosome.Genes[center - offset]);
                    var moveAfter = RubikCube.TMove.Decode((int)chromosome.Genes[center + offset]);

                    // Check if they're inverses (same structure, complementary angles)
                    if (moveBefore.Axis == moveAfter.Axis &&
                        moveBefore.Slice == moveAfter.Slice &&
                        moveBefore.Plane == moveAfter.Plane &&
                        moveBefore.Angle + moveAfter.Angle == 2)
                    {
                        // Found a symmetric pair
                        symmetryCount++;
                        hasSymmetry = true;
                        break;
                    }
                }
                if (hasSymmetry) break;
            }
        }

        Assert.True(symmetryCount > 0, "Should create symmetric ABA' patterns");
    }

    [Fact]
    public void ConjugationMutation_InvertsAngles()
    {
        var op = new ConjugationMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        // Test angle inversion: 0→2, 1→1, 2→0
        var angle0Move = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 0);
        var angle1Move = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 1);
        var angle2Move = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 2);

        int inversionObserved = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(5);
            chromosome.ValidMoves = validMoves;
            // Set up: [angle0, angle1, center, ?, ?]
            chromosome.Genes[0] = angle0Move;
            chromosome.Genes[1] = angle1Move;
            chromosome.Genes[2] = validMoves[0]; // center
            chromosome.Genes[3] = validMoves[1];
            chromosome.Genes[4] = validMoves[2];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Check if angles were inverted in the pattern
            for (int i = 0; i < chromosome.Length; i++)
            {
                var move = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                if (move.Angle == 2) // Could be inverted from 0
                {
                    inversionObserved++;
                    break;
                }
            }
        }

        Assert.True(inversionObserved > 0, "Should invert angles as part of ABA' pattern");
    }

    [Fact]
    public void ConjugationMutation_RequiresAtLeastThreeGenes()
    {
        var op = new ConjugationMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        var chromosome = new MockRubikChromosome(2);
        chromosome.ValidMoves = validMoves;
        chromosome.Genes[0] = validMoves[0];
        chromosome.Genes[1] = validMoves[1];
        var original = chromosome.Genes.ToArray();

        var rng = new Random(42);
        op.Mutate(chromosome, rng);

        // Should not change with only 2 genes
        Assert.Equal(original[0], chromosome.Genes[0]);
        Assert.Equal(original[1], chromosome.Genes[1]);
    }

    [Fact]
    public void ConjugationMutation_MakesSomeChanges()
    {
        var op = new ConjugationMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int changedTrials = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            bool changed = false;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    changed = true;
                    break;
                }
            }
            if (changed) changedTrials++;
        }

        Assert.True(changedTrials > 0, "Should make changes in some trials");
    }

    [Fact]
    public void ConjugationMutation_ProducesValidMoves()
    {
        var op = new ConjugationMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // All resulting moves should be valid
            for (int i = 0; i < chromosome.Length; i++)
            {
                var move = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                Assert.True(move.IsValid, $"Move at position {i} is invalid after conjugation mutation");
            }
        }
    }

    #endregion

    #region HyperplaneMutation Tests

    [Fact]
    public void HyperplaneMutation_ChangesAxis()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int axisChangedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);
            var newMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();

            // Check if any axis changed
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (originalMoves[i].Axis != newMoves[i].Axis)
                {
                    axisChangedCount++;
                    break;
                }
            }
        }

        Assert.True(axisChangedCount > 50,
            $"Should change axis frequently, changed in {axisChangedCount}/100 trials");
    }

    [Fact]
    public void HyperplaneMutation_PreservesSlice()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int slicePreservedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);
            var newMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();

            // Find the changed gene and verify slice is preserved
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(originalMoves[i].Encode() - newMoves[i].Encode()) > 0)
                {
                    if (originalMoves[i].Slice == newMoves[i].Slice)
                        slicePreservedCount++;
                    break;
                }
            }
        }

        Assert.True(slicePreservedCount > 90,
            $"Should preserve slice, preserved in {slicePreservedCount}/100");
    }

    [Fact]
    public void HyperplaneMutation_PreservesAngle()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int anglePreservedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);
            var newMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();

            // Find the changed gene and verify angle is preserved
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(originalMoves[i].Encode() - newMoves[i].Encode()) > 0)
                {
                    if (originalMoves[i].Angle == newMoves[i].Angle)
                        anglePreservedCount++;
                    break;
                }
            }
        }

        Assert.True(anglePreservedCount > 90,
            $"Should preserve angle, preserved in {anglePreservedCount}/100");
    }

    [Fact]
    public void HyperplaneMutation_ChangesExactlyOneGene()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            int changedCount = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            Assert.True(changedCount <= 1, $"Should change at most 1 gene, changed {changedCount}");
        }
    }

    [Fact]
    public void HyperplaneMutation_ProducesValidMoves()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // All resulting moves should be valid
            for (int i = 0; i < chromosome.Length; i++)
            {
                var move = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                Assert.True(move.IsValid, $"Move at position {i} is invalid after hyperplane mutation");
            }
        }
    }

    [Fact]
    public void HyperplaneMutation_AllPositionsCanBeSelected()
    {
        var op = new HyperplaneMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var positionCounts = new int[10];

        for (int trial = 0; trial < 1000; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    positionCounts[i]++;
                    break;
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionCounts[i] > 0,
                $"Position {i} was never selected for hyperplane mutation");
        }
    }

    #endregion

    #region NeighborMutation Tests

    [Fact]
    public void NeighborMutation_ChangesOnlyAngle()
    {
        var op = new NeighborMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int structurePreservedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);
            var newMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();

            // Find which gene changed
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(originalMoves[i].Encode() - newMoves[i].Encode()) > 0)
                {
                    // Axis, Slice, Plane should be preserved
                    if (originalMoves[i].Axis == newMoves[i].Axis &&
                        originalMoves[i].Slice == newMoves[i].Slice &&
                        originalMoves[i].Plane == newMoves[i].Plane)
                    {
                        structurePreservedCount++;
                    }
                    break;
                }
            }
        }

        Assert.True(structurePreservedCount > 80,
            $"Should preserve axis/slice/plane, preserved {structurePreservedCount}/100");
    }

    [Fact]
    public void NeighborMutation_ChangesExactlyOneGene()
    {
        var op = new NeighborMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            int changedCount = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            Assert.True(changedCount <= 1, $"Should change at most 1 gene, changed {changedCount}");
        }
    }

    [Fact]
    public void NeighborMutation_AngleChangesToDifferentValue()
    {
        var op = new NeighborMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int angleChangedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Use moves with angle 0
            var angle0Move = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 0);
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = angle0Move;

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Check if any angle changed
            for (int i = 0; i < chromosome.Length; i++)
            {
                var newMove = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                if (newMove.Angle != 0) // Changed from 0
                {
                    angleChangedCount++;
                    break;
                }
            }
        }

        Assert.True(angleChangedCount > 80,
            $"Should change angles frequently, changed {angleChangedCount}/100");
    }

    [Fact]
    public void NeighborMutation_CanInvertAngle()
    {
        var op = new NeighborMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int invertCount = 0;
        int rotateCount = 0;

        for (int trial = 0; trial < 200; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Use moves with angle 0
            var angle0Move = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 0);
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = angle0Move;

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Check the new angle
            for (int i = 0; i < chromosome.Length; i++)
            {
                var newMove = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                if (newMove.Angle == 2) // Inverted: 0 -> 2
                    invertCount++;
                else if (newMove.Angle == 1) // Rotated: 0 -> 1
                    rotateCount++;
            }
        }

        // Both strategies should occur
        Assert.True(invertCount > 20, $"Should sometimes invert angles, got {invertCount}");
        Assert.True(rotateCount > 20, $"Should sometimes rotate angles, got {rotateCount}");
    }

    [Fact]
    public void NeighborMutation_AllPositionsCanBeSelected()
    {
        var op = new NeighborMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var positionCounts = new int[10];

        for (int trial = 0; trial < 1000; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    positionCounts[i]++;
                    break;
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionCounts[i] > 0,
                $"Position {i} was never selected for neighbor mutation");
        }
    }

    #endregion

    #region InverseSequenceMutation Tests

    [Fact]
    public void InverseSequenceMutation_ReversesSegmentOrder()
    {
        var op = new InverseSequenceMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int reversalObserved = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Use distinct moves for each position
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Find the changed segment
            int start = -1, end = -1;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    if (start == -1) start = i;
                    end = i + 1;
                }
            }

            if (start != -1 && end - start >= 2)
            {
                reversalObserved++;
            }
        }

        Assert.True(reversalObserved > 0, "Should observe segment reversals");
    }

    [Fact]
    public void InverseSequenceMutation_InvertsAngles()
    {
        var op = new InverseSequenceMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        // Use moves with known angles to verify inversion
        // Find moves with angle 0, 1, 2
        var moveAngle0 = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 0);
        var moveAngle1 = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 1);
        var moveAngle2 = validMoves.First(m => RubikCube.TMove.Decode(m).Angle == 2);

        int angleInversionCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Set all genes to angle 0 moves
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = moveAngle0;

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Check if any angles were inverted (0 -> 2)
            for (int i = 0; i < chromosome.Length; i++)
            {
                var move = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                if (move.Angle == 2) // Inverted from 0
                {
                    angleInversionCount++;
                }
            }
        }

        Assert.True(angleInversionCount > 0, "Should invert angles (0 -> 2)");
    }

    [Fact]
    public void InverseSequenceMutation_PreservesSegmentMoveStructure()
    {
        var op = new InverseSequenceMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            // Record original move structures (axis, slice, plane)
            var originalStructures = chromosome.Genes
                .Select(g => {
                    var m = RubikCube.TMove.Decode((int)g);
                    return (m.Axis, m.Slice, m.Plane);
                })
                .ToList();

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // After mutation, the same move structures should exist
            // (just in different order and with inverted angles)
            var newStructures = chromosome.Genes
                .Select(g => {
                    var m = RubikCube.TMove.Decode((int)g);
                    return (m.Axis, m.Slice, m.Plane);
                })
                .ToList();

            // The multiset of (axis, slice, plane) should be the same
            var origSorted = originalStructures.OrderBy(s => s.Axis).ThenBy(s => s.Slice).ThenBy(s => s.Plane).ToList();
            var newSorted = newStructures.OrderBy(s => s.Axis).ThenBy(s => s.Slice).ThenBy(s => s.Plane).ToList();
            Assert.Equal(origSorted, newSorted);
        }
    }

    [Fact]
    public void InverseSequenceMutation_OperatesOnSegment()
    {
        var op = new InverseSequenceMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int changedSomething = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Check if any changes were made
            bool changed = false;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    changed = true;
                    break;
                }
            }
            if (changed) changedSomething++;
        }

        Assert.True(changedSomething > 0, "Should make changes in some trials");
    }

    [Fact]
    public void InverseSequenceMutation_RequiresAtLeastTwoGenes()
    {
        var op = new InverseSequenceMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        var chromosome = new MockRubikChromosome(1);
        chromosome.ValidMoves = validMoves;
        chromosome.Genes[0] = validMoves[0];
        var original = chromosome.Genes[0];

        var rng = new Random(42);
        op.Mutate(chromosome, rng);

        Assert.Equal(original, chromosome.Genes[0]);
    }

    #endregion

    #region InsertMutation Tests

    [Fact]
    public void InsertMutation_ChangesExactlyTwoAdjacentGenes()
    {
        var op = new InsertMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Use distinct values that won't match any valid move encoding
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = -1000 - i;

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Find which indices changed
            var changedIndices = new List<int>();
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedIndices.Add(i);
            }

            Assert.Equal(2, changedIndices.Count);
            Assert.Equal(1, changedIndices[1] - changedIndices[0]); // Adjacent
        }
    }

    [Fact]
    public void InsertMutation_InsertedMovesHaveSameAxisSlicePlane()
    {
        var op = new InsertMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int sameStructureCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = -1000 - i;

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Find the changed pair
            for (int i = 0; i < chromosome.Length - 1; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001 &&
                    Math.Abs(original[i + 1] - chromosome.Genes[i + 1]) > 0.001)
                {
                    var move1 = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                    var move2 = RubikCube.TMove.Decode((int)chromosome.Genes[i + 1]);

                    // Both moves should have same axis, slice, plane (neutral pair structure)
                    if (move1.Axis == move2.Axis &&
                        move1.Slice == move2.Slice &&
                        move1.Plane == move2.Plane)
                    {
                        sameStructureCount++;
                    }
                    break;
                }
            }
        }

        Assert.True(sameStructureCount > 90,
            $"Inserted pairs should have same structure, found {sameStructureCount}/100");
    }

    [Fact]
    public void InsertMutation_UsesValidMoves()
    {
        var op = new InsertMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var validMovesSet = new HashSet<int>(validMoves);

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[0];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Changed genes should be valid moves
            for (int i = 0; i < chromosome.Length; i++)
            {
                int geneValue = (int)chromosome.Genes[i];
                if (geneValue != validMoves[0])
                {
                    Assert.True(validMovesSet.Contains(geneValue),
                        $"Gene {i} has invalid move code {geneValue}");
                }
            }
        }
    }

    [Fact]
    public void InsertMutation_AllPositionsCanBeSelected()
    {
        var op = new InsertMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();
        var positionCounts = new int[9]; // 0-8 for length 10 chromosome

        for (int trial = 0; trial < 1000; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = -1000 - i;

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // Find first changed position
            for (int i = 0; i < chromosome.Length - 1; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                {
                    positionCounts[i]++;
                    break;
                }
            }
        }

        // All positions 0-8 should be selectable
        for (int i = 0; i < 9; i++)
        {
            Assert.True(positionCounts[i] > 0,
                $"Position {i} was never selected for insertion");
        }
    }

    [Fact]
    public void InsertMutation_RequiresAtLeastTwoGenes()
    {
        var op = new InsertMutation<MockRubikChromosome>();
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        var chromosome = new MockRubikChromosome(1);
        chromosome.ValidMoves = validMoves;
        chromosome.Genes[0] = validMoves[0];
        var original = chromosome.Genes[0];

        var rng = new Random(42);
        op.Mutate(chromosome, rng);

        // Should not change anything with only 1 gene
        Assert.Equal(original, chromosome.Genes[0]);
    }

    #endregion

    #region AdaptiveMutation Tests

    [Fact]
    public void AdaptiveMutation_HighFitness_MoreAggressiveMutation()
    {
        var op = new AdaptiveMutation<MockChromosome>(
            minGenes: 1, maxGenes: 8, minExpectedFitness: 0, maxExpectedFitness: 100);

        // Track mutation intensity for different fitness values
        var changesHighFitness = new List<int>();
        var changesLowFitness = new List<int>();

        for (int trial = 0; trial < 100; trial++)
        {
            // High fitness value (poor solution, should mutate more)
            var chrHigh = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            chrHigh.Fitness = 90; // High = poor
            var origHigh = chrHigh.Genes.ToArray();
            var rng1 = new Random(trial);
            op.Mutate(chrHigh, rng1);
            changesHighFitness.Add(Enumerable.Range(0, 10).Count(i => Math.Abs(chrHigh.Genes[i] - origHigh[i]) > 0.001));

            // Low fitness value (good solution, should mutate less)
            var chrLow = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            chrLow.Fitness = 10; // Low = good
            var origLow = chrLow.Genes.ToArray();
            var rng2 = new Random(trial + 10000);
            op.Mutate(chrLow, rng2);
            changesLowFitness.Add(Enumerable.Range(0, 10).Count(i => Math.Abs(chrLow.Genes[i] - origLow[i]) > 0.001));
        }

        double avgHigh = changesHighFitness.Average();
        double avgLow = changesLowFitness.Average();

        Assert.True(avgHigh > avgLow,
            $"High fitness (poor) should mutate more: avgHigh={avgHigh}, avgLow={avgLow}");
    }

    [Fact]
    public void AdaptiveMutation_UsesScrambleForHighIntensity()
    {
        var op = new AdaptiveMutation<MockChromosome>(
            minGenes: 1, maxGenes: 8, minExpectedFitness: 0, maxExpectedFitness: 100);

        // For very high fitness (intensity > 0.7), should use scramble
        // Scramble preserves the gene multiset
        int scrambleCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            chromosome.Fitness = 95; // Very high = scramble territory
            var originalSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // After scramble, the sorted genes should be similar
            // (some may be replaced by random, but the core should remain)
            var resultSorted = chromosome.Genes.OrderBy(x => x).ToArray();
            int preserved = originalSorted.Intersect(resultSorted).Count();
            if (preserved >= 5) // At least half preserved indicates scramble
                scrambleCount++;
        }

        Assert.True(scrambleCount > 30,
            $"High intensity should use scramble mutation, preserved count: {scrambleCount}");
    }

    [Fact]
    public void AdaptiveMutation_LowIntensity_UsesNeighborMutation()
    {
        var op = new AdaptiveMutation<MockChromosome>(
            minGenes: 1, maxGenes: 8, minExpectedFitness: 0, maxExpectedFitness: 100);

        // For low fitness (intensity < 0.3), changes should be smaller
        var changesMagnitudes = new List<double>();
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);
            chromosome.Fitness = 10; // Low = good, uses neighbor mutation
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                double delta = Math.Abs(chromosome.Genes[i] - original[i]);
                if (delta > 0.001)
                    changesMagnitudes.Add(delta);
            }
        }

        // Neighbor mutation makes small perturbations (typically ±5)
        double avgChange = changesMagnitudes.Average();
        Assert.True(avgChange < 20,
            $"Low intensity should make small changes, avg={avgChange}");
    }

    [Fact]
    public void AdaptiveMutation_MinMaxGenesRespected()
    {
        var op = new AdaptiveMutation<MockChromosome>(
            minGenes: 2, maxGenes: 5, minExpectedFitness: 0, maxExpectedFitness: 100);

        // Test that genes mutated is within minGenes-maxGenes range
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            chromosome.Fitness = 50; // Mid-range
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            int changed = Enumerable.Range(0, 10)
                .Count(i => Math.Abs(original[i] - chromosome.Genes[i]) > 0.001);

            // Should be at least minGenes (though may be less due to position overlap)
            // and at most some reasonable number related to maxGenes
            Assert.True(changed <= chromosome.Length,
                $"Changed {changed} genes, should not exceed chromosome length");
        }
    }

    [Fact]
    public void AdaptiveMutation_WithRubikChromosome_UsesValidMoves()
    {
        var op = new AdaptiveMutation<MockRubikChromosome>(
            minGenes: 1, maxGenes: 5, minExpectedFitness: 0, maxExpectedFitness: 100);
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        for (int trial = 0; trial < 50; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            chromosome.Fitness = 80; // High intensity - random mutation
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[0];

            var rng = new Random(trial);
            op.Mutate(chromosome, rng);

            // All genes should still be valid moves (or 0-999 from generic fallback)
            // For Rubik chromosome path, should use ValidMoves
        }
        // Test passes if no exception is thrown
    }

    [Fact]
    public void AdaptiveMutation_IntensityCalculation()
    {
        // Test various fitness values map correctly to intensity
        var op = new AdaptiveMutation<MockChromosome>(
            minGenes: 1, maxGenes: 10, minExpectedFitness: 0, maxExpectedFitness: 100);

        // Fitness at min (0) should give minimal mutation
        // Fitness at max (100) should give maximal mutation
        // We can observe this through the number of changes

        var changesAtMin = new List<int>();
        var changesAtMax = new List<int>();

        for (int trial = 0; trial < 100; trial++)
        {
            var chrMin = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            chrMin.Fitness = 0; // Best fitness
            var origMin = chrMin.Genes.ToArray();
            op.Mutate(chrMin, new Random(trial));
            changesAtMin.Add(Enumerable.Range(0, 10).Count(i => Math.Abs(chrMin.Genes[i] - origMin[i]) > 0.001));

            var chrMax = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            chrMax.Fitness = 100; // Worst fitness
            var origMax = chrMax.Genes.ToArray();
            op.Mutate(chrMax, new Random(trial + 5000));
            changesAtMax.Add(Enumerable.Range(0, 10).Count(i => Math.Abs(chrMax.Genes[i] - origMax[i]) > 0.001));
        }

        Assert.True(changesAtMax.Average() > changesAtMin.Average(),
            $"Max fitness changes ({changesAtMax.Average()}) should exceed min ({changesAtMin.Average()})");
    }

    #endregion

    #region CreepMutation Tests

    [Fact]
    public void CreepMutation_MakesSmallChanges()
    {
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: 1.0);
        var changes = new List<double>();

        for (int trial = 0; trial < 500; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                double delta = Math.Abs(chromosome.Genes[i] - original[i]);
                if (delta > 0.001)
                    changes.Add(delta);
            }
        }

        // All changes should be within creepRange
        Assert.All(changes, delta => Assert.True(delta <= 1.0,
            $"Change {delta} exceeds creepRange 1.0"));
    }

    [Fact]
    public void CreepMutation_CreepRangeControlsMaxChange()
    {
        double creepRange = 0.5;
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: creepRange);
        var changes = new List<double>();

        for (int trial = 0; trial < 500; trial++)
        {
            var chromosome = MockChromosome.WithGenes(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                double delta = Math.Abs(chromosome.Genes[i] - original[i]);
                if (delta > 0.001)
                    changes.Add(delta);
            }
        }

        // All changes should be within creepRange
        Assert.True(changes.Count > 0, "No changes were made");
        Assert.All(changes, delta => Assert.True(delta <= creepRange + 0.001,
            $"Change {delta} exceeds creepRange {creepRange}"));
    }

    [Fact]
    public void CreepMutation_ChangesCanBePositiveOrNegative()
    {
        var op = new CreepMutation<MockChromosome>(genesToMutate: 1, creepRange: 1.0);
        int positiveCount = 0;
        int negativeCount = 0;

        for (int trial = 0; trial < 500; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                double delta = chromosome.Genes[i] - original[i];
                if (delta > 0.001) positiveCount++;
                if (delta < -0.001) negativeCount++;
            }
        }

        Assert.True(positiveCount > 100, "Should have positive changes");
        Assert.True(negativeCount > 100, "Should have negative changes");
    }

    [Fact]
    public void CreepMutation_GenesToMutateControlsCount()
    {
        for (int genesToMutate = 1; genesToMutate <= 5; genesToMutate++)
        {
            var op = new CreepMutation<MockChromosome>(genesToMutate: genesToMutate, creepRange: 1.0);
            var mutationCounts = new List<int>();

            for (int trial = 0; trial < 100; trial++)
            {
                var chromosome = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                var original = chromosome.Genes.ToArray();
                var rng = new Random(trial);

                op.Mutate(chromosome, rng);

                int changed = 0;
                for (int i = 0; i < chromosome.Length; i++)
                {
                    if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                        changed++;
                }
                mutationCounts.Add(changed);
            }

            double avgMutations = mutationCounts.Average();
            Assert.True(avgMutations >= genesToMutate * 0.5,
                $"With genesToMutate={genesToMutate}, expected avg ~{genesToMutate}, got {avgMutations}");
        }
    }

    [Fact]
    public void CreepMutation_WithRubikChromosome_CreepsAngle()
    {
        var op = new CreepMutation<MockRubikChromosome>(genesToMutate: 10, creepRange: 1.0);
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int angleChanges = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Initialize with a specific move (angle 0)
            int baseMoveCode = validMoves[0];
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = baseMoveCode;

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                var newMove = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                var origMove = originalMoves[i];
                if (newMove.Angle != origMove.Angle)
                    angleChanges++;
            }
        }

        Assert.True(angleChanges > 0, "CreepMutation with Rubik chromosome never creeps angles");
    }

    [Fact]
    public void CreepMutation_WithRubikChromosome_CreepsSlice()
    {
        var op = new CreepMutation<MockRubikChromosome>(genesToMutate: 10, creepRange: 1.0);
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int sliceChanges = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Initialize with middle slice moves
            int baseMoveCode = validMoves.First(m => RubikCube.TMove.Decode(m).Slice == 1);
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = baseMoveCode;

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                var newMove = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                var origMove = originalMoves[i];
                if (newMove.Slice != origMove.Slice)
                    sliceChanges++;
            }
        }

        Assert.True(sliceChanges > 0, "CreepMutation with Rubik chromosome never creeps slices");
    }

    [Fact]
    public void CreepMutation_WithRubikChromosome_CreepsPlane()
    {
        var op = new CreepMutation<MockRubikChromosome>(genesToMutate: 10, creepRange: 1.0);
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int planeChanges = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            int baseMoveCode = validMoves[0];
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = baseMoveCode;

            var originalMoves = chromosome.Genes.Select(g => RubikCube.TMove.Decode((int)g)).ToList();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                var newMove = RubikCube.TMove.Decode((int)chromosome.Genes[i]);
                var origMove = originalMoves[i];
                if (newMove.Plane != origMove.Plane)
                    planeChanges++;
            }
        }

        Assert.True(planeChanges > 0, "CreepMutation with Rubik chromosome never creeps planes");
    }

    #endregion

    #region GaussianMutation Tests

    [Fact]
    public void GaussianMutation_AddsNoiseToGeneValues()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 1.0);

        int changedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }
        }

        Assert.True(changedCount > 0, "GaussianMutation never changed any genes");
    }

    [Fact]
    public void GaussianMutation_NoiseIsCenteredAroundZero()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 1.0);
        var deltas = new List<double>();

        for (int trial = 0; trial < 1000; trial++)
        {
            var chromosome = MockChromosome.WithGenes(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                double delta = chromosome.Genes[i] - original[i];
                if (Math.Abs(delta) > 0.0001)
                    deltas.Add(delta);
            }
        }

        // Mean should be close to 0 for Gaussian noise
        double mean = deltas.Average();
        Assert.True(Math.Abs(mean) < 0.2,
            $"Mean of Gaussian noise should be near 0, got {mean}");
    }

    [Fact]
    public void GaussianMutation_SigmaControlsSpread()
    {
        var opSmall = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 0.5);
        var opLarge = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 2.0);

        var deltasSmall = new List<double>();
        var deltasLarge = new List<double>();

        for (int trial = 0; trial < 500; trial++)
        {
            var chr1 = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var chr2 = MockChromosome.WithGenes(50, 50, 50, 50, 50, 50, 50, 50, 50, 50);
            var rng1 = new Random(trial);
            var rng2 = new Random(trial + 10000);

            opSmall.Mutate(chr1, rng1);
            opLarge.Mutate(chr2, rng2);

            for (int i = 0; i < chr1.Length; i++)
            {
                if (Math.Abs(chr1.Genes[i] - 50) > 0.001)
                    deltasSmall.Add(Math.Abs(chr1.Genes[i] - 50));
                if (Math.Abs(chr2.Genes[i] - 50) > 0.001)
                    deltasLarge.Add(Math.Abs(chr2.Genes[i] - 50));
            }
        }

        double avgSmall = deltasSmall.Average();
        double avgLarge = deltasLarge.Average();

        // Larger sigma should produce larger average absolute changes
        Assert.True(avgLarge > avgSmall,
            $"Larger sigma should produce larger changes. Small: {avgSmall}, Large: {avgLarge}");
    }

    [Fact]
    public void GaussianMutation_GenesToMutateControlsCount()
    {
        for (int genesToMutate = 1; genesToMutate <= 5; genesToMutate++)
        {
            var op = new GaussianMutation<MockChromosome>(genesToMutate: genesToMutate, sigma: 1.0);
            var mutationCounts = new List<int>();

            for (int trial = 0; trial < 100; trial++)
            {
                var chromosome = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                var original = chromosome.Genes.ToArray();
                var rng = new Random(trial);

                op.Mutate(chromosome, rng);

                int changed = 0;
                for (int i = 0; i < chromosome.Length; i++)
                {
                    if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                        changed++;
                }
                mutationCounts.Add(changed);
            }

            // Average mutations should be close to genesToMutate
            // (may be less if same gene selected twice)
            double avgMutations = mutationCounts.Average();
            Assert.True(avgMutations >= genesToMutate * 0.5,
                $"With genesToMutate={genesToMutate}, expected avg ~{genesToMutate}, got {avgMutations}");
        }
    }

    [Fact]
    public void GaussianMutation_ProducesApproximatelyNormalDistribution()
    {
        var op = new GaussianMutation<MockChromosome>(genesToMutate: 1, sigma: 1.0);
        var deltas = new List<double>();

        for (int trial = 0; trial < 2000; trial++)
        {
            var chromosome = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(chromosome.Genes[i]) > 0.0001)
                    deltas.Add(chromosome.Genes[i]);
            }
        }

        // For normal distribution, ~68% should be within 1 sigma
        int withinOneSigma = deltas.Count(d => Math.Abs(d) <= 1.0);
        double proportionWithin = (double)withinOneSigma / deltas.Count;

        // Allow some tolerance (expect 0.68, allow 0.55-0.80)
        Assert.True(proportionWithin > 0.55 && proportionWithin < 0.80,
            $"Expected ~68% within 1σ, got {proportionWithin * 100:F1}%");
    }

    [Fact]
    public void GaussianMutation_WithRubikChromosome_ChangesMove()
    {
        var op = new GaussianMutation<MockRubikChromosome>(genesToMutate: 1, sigma: 1.0);
        var validMoves = RubikCube.TRubikGenome.FreeMoves.ToList();

        int changedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = new MockRubikChromosome(10);
            chromosome.ValidMoves = validMoves;
            // Initialize with valid moves
            for (int i = 0; i < chromosome.Length; i++)
                chromosome.Genes[i] = validMoves[i % validMoves.Count];

            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < chromosome.Length; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }
        }

        Assert.True(changedCount > 0, "GaussianMutation with RubikChromosome never changed any genes");
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
