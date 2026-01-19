using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Translocation mutation - swaps two non-overlapping segments.
///
/// This mutation exchanges the positions of two distinct segments within the chromosome.
/// Unlike simple swap (which exchanges single genes), translocation exchanges
/// contiguous blocks of genes.
///
/// Algorithm:
/// 1. Select first segment [start1, end1)
/// 2. Select second segment [start2, end2) that doesn't overlap with first
/// 3. Exchange the two segments
///
/// Example:
/// Original:  [A B C D E F G H I J]
/// Segment1:  [B C] (positions 1-2)
/// Segment2:  [F G H] (positions 5-7)
/// Result:    [A F G H D E B C I J]
///
/// Note: When segments have different sizes, the chromosome structure adjusts
/// to accommodate both segments in their new positions.
///
/// For Rubik's cube: This can swap two "sub-algorithms" within a solution,
/// potentially discovering that a different arrangement of move sequences
/// is more effective.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class TranslocationMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _minSegmentSize;
    private readonly int _maxSegmentSize;

    /// <summary>
    /// Creates a translocation mutation operator.
    /// </summary>
    /// <param name="minSegmentSize">Minimum segment size. Default: 2</param>
    /// <param name="maxSegmentSize">Maximum segment size. Default: 4</param>
    public TranslocationMutation(int minSegmentSize = 2, int maxSegmentSize = 4)
    {
        _minSegmentSize = Math.Max(1, minSegmentSize);
        _maxSegmentSize = Math.Max(_minSegmentSize, maxSegmentSize);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 4) return; // Need at least 4 genes for two non-overlapping segments

        // Determine maximum possible segment size (need room for two segments)
        int maxPossibleSize = Math.Min(_maxSegmentSize, length / 2 - 1);
        if (maxPossibleSize < _minSegmentSize) return;

        // Select first segment
        int size1 = rng.Next(_minSegmentSize, maxPossibleSize + 1);
        int start1 = rng.Next(0, length - size1 * 2); // Leave room for second segment
        int end1 = start1 + size1;

        // Select second segment (must not overlap with first)
        int remainingLength = length - end1;
        int maxSize2 = Math.Min(_maxSegmentSize, remainingLength);
        if (maxSize2 < _minSegmentSize) return;

        int size2 = rng.Next(_minSegmentSize, maxSize2 + 1);
        int start2 = rng.Next(end1, length - size2 + 1);
        int end2 = start2 + size2;

        // Extract both segments
        var segment1 = new double[size1];
        var segment2 = new double[size2];
        Array.Copy(chromosome.Genes, start1, segment1, 0, size1);
        Array.Copy(chromosome.Genes, start2, segment2, 0, size2);

        // Also extract the middle part (between segments)
        int middleSize = start2 - end1;
        var middle = new double[middleSize];
        if (middleSize > 0)
        {
            Array.Copy(chromosome.Genes, end1, middle, 0, middleSize);
        }

        // Reconstruct: [before seg1] + [seg2] + [middle] + [seg1] + [after seg2]
        int writePos = start1;

        // Write segment2 at segment1's position
        Array.Copy(segment2, 0, chromosome.Genes, writePos, size2);
        writePos += size2;

        // Write middle part
        if (middleSize > 0)
        {
            Array.Copy(middle, 0, chromosome.Genes, writePos, middleSize);
            writePos += middleSize;
        }

        // Write segment1 at segment2's position
        Array.Copy(segment1, 0, chromosome.Genes, writePos, size1);
    }
}
