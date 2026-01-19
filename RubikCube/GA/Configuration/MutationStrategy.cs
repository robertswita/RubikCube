namespace TGL.GA.Configuration;

/// <summary>
/// Available mutation strategies for introducing variation.
/// </summary>
public enum MutationStrategy
{
    /// <summary>
    /// Single gene mutation - changes exactly one random gene (matches original TGA behavior).
    /// </summary>
    SingleGene,

    /// <summary>
    /// Random mutation - replaces a gene with a random valid value.
    /// </summary>
    Random,

    /// <summary>
    /// Conjugation mutation - domain-specific for Rubik's cube (ABA^-1B^-1 pattern).
    /// </summary>
    Conjugation,

    /// <summary>
    /// Swap mutation - exchanges two genes.
    /// </summary>
    Swap,

    /// <summary>
    /// Inversion mutation - reverses a random subsequence.
    /// </summary>
    Inversion,

    /// <summary>
    /// Scramble mutation - shuffles a random subsequence.
    /// </summary>
    Scramble,

    /// <summary>
    /// Adaptive mutation - rate varies based on fitness (higher fitness = lower rate).
    /// </summary>
    Adaptive,

    /// <summary>
    /// Commutator mutation - domain-specific for Rubik's cube (ABA'B' pattern).
    /// Affects only a small number of pieces, useful for precise solving.
    /// </summary>
    Commutator,

    /// <summary>
    /// Neighbor mutation - changes a move to a similar one (same axis, different angle).
    /// </summary>
    Neighbor,

    /// <summary>
    /// Simplify mutation - detects and removes redundant move patterns.
    /// </summary>
    Simplify,

    /// <summary>
    /// Inverse sequence mutation - replaces a segment with its inverse.
    /// </summary>
    InverseSequence,

    /// <summary>
    /// Insert mutation - inserts a neutral move pair at a random position.
    /// </summary>
    Insert,

    /// <summary>
    /// Shift mutation (circular rotation) - rotates genes in the chromosome.
    /// Example: abcdef -> fabcde (right shift) or abcdef -> bcdefa (left shift).
    /// Highly disruptive mutation useful for escaping local minima.
    /// </summary>
    Shift,

    /// <summary>
    /// Displacement mutation - removes a segment and inserts it at another position.
    /// Preserves all genes but changes their arrangement.
    /// </summary>
    Displacement,

    /// <summary>
    /// Translocation mutation - swaps two non-overlapping segments.
    /// Exchanges positions of two distinct gene blocks.
    /// </summary>
    Translocation,

    /// <summary>
    /// Creep mutation - makes small incremental changes to genes.
    /// For Rubik's cube: changes angles by ±1, slices by ±1.
    /// For continuous: adds small random values.
    /// </summary>
    Creep,

    /// <summary>
    /// Gaussian mutation - adds Gaussian (normal) distributed noise.
    /// Noise magnitude determines mutation intensity.
    /// Common in evolution strategies (ES).
    /// </summary>
    Gaussian
}
