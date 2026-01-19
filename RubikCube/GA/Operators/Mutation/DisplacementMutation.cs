using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Displacement mutation - removes a segment from one position and inserts it at another.
///
/// This mutation preserves all genetic material but changes its arrangement.
/// Unlike swap or inversion, displacement moves a contiguous block to a new location,
/// shifting other genes to accommodate.
///
/// Algorithm:
/// 1. Select a random segment [start, end)
/// 2. Select a random insertion point outside the segment
/// 3. Remove the segment and insert it at the new position
///
/// Example:
/// Original:  [A B C D E F G H]
/// Segment:   [C D E] (positions 2-4)
/// Insert at: position 6
/// Result:    [A B F G C D E H]
///
/// For Rubik's cube: This operator reorganizes the sequence of moves without
/// changing what moves are present. Since move order matters significantly,
/// this can discover better orderings of the same move set.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class DisplacementMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _minSegmentSize;
    private readonly int _maxSegmentSize;

    /// <summary>
    /// Creates a displacement mutation operator.
    /// </summary>
    /// <param name="minSegmentSize">Minimum segment size to displace. Default: 2</param>
    /// <param name="maxSegmentSize">Maximum segment size to displace. Default: 5</param>
    public DisplacementMutation(int minSegmentSize = 2, int maxSegmentSize = 5)
    {
        _minSegmentSize = Math.Max(1, minSegmentSize);
        _maxSegmentSize = Math.Max(_minSegmentSize, maxSegmentSize);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 3) return; // Need at least 3 genes to displace

        // Determine segment size
        int maxPossibleSize = Math.Min(_maxSegmentSize, length - 1);
        int minSize = Math.Min(_minSegmentSize, maxPossibleSize);
        int segmentSize = rng.Next(minSize, maxPossibleSize + 1);

        // Select segment start position
        int segmentStart = rng.Next(0, length - segmentSize);
        int segmentEnd = segmentStart + segmentSize;

        // Extract the segment
        var segment = new double[segmentSize];
        Array.Copy(chromosome.Genes, segmentStart, segment, 0, segmentSize);

        // Select insertion point (must be outside the segment)
        // After removal, the valid insertion points are 0 to (length - segmentSize)
        int insertionPoint;
        do
        {
            insertionPoint = rng.Next(0, length - segmentSize + 1);
        } while (insertionPoint >= segmentStart && insertionPoint <= segmentStart);

        // Perform the displacement
        if (insertionPoint < segmentStart)
        {
            // Move genes between insertion point and segment start to the right
            Array.Copy(chromosome.Genes, insertionPoint,
                       chromosome.Genes, insertionPoint + segmentSize,
                       segmentStart - insertionPoint);
            // Insert segment at new position
            Array.Copy(segment, 0, chromosome.Genes, insertionPoint, segmentSize);
        }
        else
        {
            // insertionPoint > segmentStart
            // Adjust insertion point for the gap created by removal
            int adjustedInsert = insertionPoint;

            // Shift genes from after segment to fill the gap
            Array.Copy(chromosome.Genes, segmentEnd,
                       chromosome.Genes, segmentStart,
                       adjustedInsert - segmentStart);

            // Insert segment at new position
            Array.Copy(segment, 0, chromosome.Genes, adjustedInsert, segmentSize);
        }
    }
}
