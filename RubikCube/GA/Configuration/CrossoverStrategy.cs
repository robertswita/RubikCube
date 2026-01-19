namespace TGL.GA.Configuration;

/// <summary>
/// Available crossover strategies for combining parents.
/// </summary>
public enum CrossoverStrategy
{
    /// <summary>
    /// Single-point crossover - splits at one random point.
    /// </summary>
    SinglePoint,

    /// <summary>
    /// Two-point crossover - swaps segment between two random points.
    /// </summary>
    TwoPoint,

    /// <summary>
    /// Uniform crossover - each gene independently chosen from either parent.
    /// </summary>
    Uniform,

    /// <summary>
    /// Order-based crossover (OX) - preserves relative order, good for permutations.
    /// </summary>
    OrderBased,

    /// <summary>
    /// Partially Mapped Crossover (PMX) - maintains position-based relationships.
    /// </summary>
    PartiallyMapped,

    /// <summary>
    /// Segment-preserving crossover - preserves effective segments from better parent.
    /// </summary>
    SegmentPreserving
}
