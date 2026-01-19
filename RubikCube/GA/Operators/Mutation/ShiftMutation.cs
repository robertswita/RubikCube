using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Shift mutation (also known as Rotation or Circular Shift mutation).
/// Performs a circular rotation of genes in the chromosome.
///
/// Example: abcdef -> fabcde (shift right by 1) or abcdef -> bcdefa (shift left by 1)
///
/// This mutation is commonly used in permutation problems like TSP where the
/// starting point doesn't matter. For Rubik's cube, it's a highly disruptive
/// mutation that can help escape local minima, though the result will typically
/// be very different from the original since cube moves don't commute.
///
/// The operator supports:
/// - Full chromosome shifts (circular rotation of all genes)
/// - Partial segment shifts (circular rotation within a segment)
/// - Random shift amounts (1 to length-1 positions)
/// - Both left and right shift directions
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class ShiftMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly bool _segmentOnly;
    private readonly double _segmentProbability;

    /// <summary>
    /// Creates a new shift mutation operator.
    /// </summary>
    /// <param name="segmentOnly">If true, only shifts a random segment instead of the whole chromosome.</param>
    /// <param name="segmentProbability">When segmentOnly is false, probability of shifting just a segment (0.0 to 1.0).</param>
    public ShiftMutation(bool segmentOnly = false, double segmentProbability = 0.3)
    {
        _segmentOnly = segmentOnly;
        _segmentProbability = Math.Clamp(segmentProbability, 0.0, 1.0);
    }

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Decide whether to shift the whole chromosome or just a segment
        bool shiftSegment = _segmentOnly || rng.NextDouble() < _segmentProbability;

        if (shiftSegment)
        {
            ShiftSegment(chromosome, rng, length);
        }
        else
        {
            ShiftFull(chromosome, rng, length);
        }
    }

    /// <summary>
    /// Performs a circular shift on the entire chromosome.
    /// </summary>
    private void ShiftFull(T chromosome, Random rng, int length)
    {
        // Random shift amount (1 to length-1, shifting by 0 or length would be no change)
        int shiftAmount = rng.Next(1, length);

        // Decide direction: positive = right shift, negative = left shift
        bool shiftRight = rng.NextDouble() < 0.5;

        if (!shiftRight)
        {
            // Convert left shift to equivalent right shift
            // Left shift by k = Right shift by (length - k)
            shiftAmount = length - shiftAmount;
        }

        // Perform the circular shift using reversal algorithm (efficient, in-place)
        // To shift right by k: reverse whole array, reverse first k, reverse rest
        // Example: abcdef, shift right by 2:
        //   reverse all: fedcba
        //   reverse [0..2): efdcba
        //   reverse [2..6): efabcd ✓
        CircularShift(chromosome, 0, length, shiftAmount);
    }

    /// <summary>
    /// Performs a circular shift on a random segment of the chromosome.
    /// </summary>
    private void ShiftSegment(T chromosome, Random rng, int length)
    {
        // Select a random segment (at least 2 elements)
        int start = rng.Next(length - 1);
        int minEnd = start + 2;
        int end = rng.Next(minEnd, length + 1);
        int segmentLength = end - start;

        if (segmentLength < 2) return;

        // Random shift amount within segment
        int shiftAmount = rng.Next(1, segmentLength);

        CircularShift(chromosome, start, segmentLength, shiftAmount);
    }

    /// <summary>
    /// Performs a circular right shift using the reversal algorithm.
    /// This is an efficient O(n) in-place algorithm.
    ///
    /// To shift right by k positions:
    /// 1. Reverse the entire segment
    /// 2. Reverse the first k elements
    /// 3. Reverse the remaining elements
    /// </summary>
    private void CircularShift(T chromosome, int start, int length, int shiftAmount)
    {
        // Normalize shift amount
        shiftAmount = shiftAmount % length;
        if (shiftAmount == 0) return;

        // Reversal algorithm for circular shift
        Reverse(chromosome, start, start + length - 1);
        Reverse(chromosome, start, start + shiftAmount - 1);
        Reverse(chromosome, start + shiftAmount, start + length - 1);
    }

    /// <summary>
    /// Reverses elements in the chromosome between indices left and right (inclusive).
    /// </summary>
    private void Reverse(T chromosome, int left, int right)
    {
        while (left < right)
        {
            // Swap genes[left] and genes[right]
            var temp = chromosome.Genes[left];
            chromosome.Genes[left] = chromosome.Genes[right];
            chromosome.Genes[right] = temp;
            left++;
            right--;
        }
    }
}
