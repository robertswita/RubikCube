using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA;

/// <summary>
/// Fitness evaluator for Rubik's cube genomes.
/// Evaluates how close a sequence of moves brings the cube to being solved.
/// </summary>
public class RubikFitnessEvaluator : MinimizingFitnessEvaluator<TRubikGenome>
{
    private readonly TRubikCube _baseCube;

    /// <summary>
    /// Creates a fitness evaluator for the given cube state.
    /// </summary>
    /// <param name="baseCube">The cube to evaluate moves against.</param>
    public RubikFitnessEvaluator(TRubikCube baseCube)
    {
        _baseCube = baseCube;
    }

    /// <summary>
    /// Evaluates a genome by applying its moves to the cube and finding the best fitness.
    /// </summary>
    /// <param name="chromosome">The genome containing the move sequence.</param>
    /// <returns>The best fitness achieved at any point in the move sequence.</returns>
    public override double Evaluate(TRubikGenome chromosome)
    {
        chromosome.Check();
        chromosome.Fitness = double.MaxValue;

        // Create a copy of the cube for evaluation
        var cube = new TRubikCube(_baseCube);

        for (int i = 0; i < chromosome.Genes.Length; i++)
        {
            var move = TMove.Decode((int)chromosome.Genes[i]);
            cube.Turn(move);

            double fitness = cube.Evaluate();
            if (fitness < chromosome.Fitness)
            {
                chromosome.Fitness = fitness;
                chromosome.MovesCount = i + 1;
            }
        }

        return chromosome.Fitness;
    }
}
