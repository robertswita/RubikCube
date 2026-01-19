using System;
using System.Collections.Generic;

namespace TGL.GA.Interfaces;

/// <summary>
/// Base interface for all chromosome types in the genetic algorithm.
/// Represents an individual solution in the population.
/// </summary>
public interface IChromosome : IComparable<IChromosome>, ICloneable
{
    /// <summary>
    /// The fitness value of this chromosome. Lower values indicate better solutions.
    /// </summary>
    double Fitness { get; set; }

    /// <summary>
    /// The genes (solution encoding) of this chromosome.
    /// </summary>
    double[] Genes { get; }

    /// <summary>
    /// The number of genes in this chromosome.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Initializes the chromosome with random genes.
    /// </summary>
    /// <param name="rng">Random number generator to use.</param>
    void Randomize(Random rng);

    /// <summary>
    /// Validates and cleans up the chromosome (removes redundant genes, etc.).
    /// </summary>
    void Validate();
}

/// <summary>
/// Specialized chromosome interface for Rubik's cube solving.
/// </summary>
public interface IRubikChromosome : IChromosome
{
    /// <summary>
    /// The number of moves that achieved the best fitness during evaluation.
    /// </summary>
    int MovesCount { get; set; }

    /// <summary>
    /// The list of valid move codes for the current solving context.
    /// </summary>
    IReadOnlyList<int> ValidMoves { get; set; }
}
