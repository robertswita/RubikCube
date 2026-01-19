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
    Adaptive
}
