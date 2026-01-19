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
    Insert
}
