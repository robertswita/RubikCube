using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RubikCube;

namespace TGL.GA;

/// <summary>
/// Database of previously found solutions that can be reused to speed up solving.
/// Solutions are stored with position-independent encoding and translated at runtime.
/// </summary>
public class SolutionDatabase
{
    private readonly Dictionary<string, List<TMove>> _solutions = new();
    private readonly string _filePath;
    private readonly object _lock = new();

    /// <summary>
    /// Number of solutions in the database.
    /// </summary>
    public int Count
    {
        get { lock (_lock) return _solutions.Count; }
    }

    /// <summary>
    /// Event raised when a new solution is saved.
    /// </summary>
    public event Action<int>? SolutionSaved;

    /// <summary>
    /// Creates a new solution database.
    /// </summary>
    /// <param name="filePath">Path to the solutions file.</param>
    public SolutionDatabase(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Loads solutions from the database file.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            _solutions.Clear();
            try
            {
                if (!File.Exists(_filePath)) return;

                using var file = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(file);

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    var key = reader.ReadString();
                    var movesCount = reader.ReadInt32();
                    var solution = new List<TMove>(movesCount);

                    for (int i = 0; i < movesCount; i++)
                    {
                        var move = new TMove
                        {
                            Axis = reader.ReadByte(),
                            Slice = reader.ReadByte(),
                            Plane = reader.ReadByte(),
                            Angle = reader.ReadByte()
                        };
                        solution.Add(move);
                    }

                    _solutions[key] = solution;
                }
            }
            catch (Exception)
            {
                // Ignore errors - database may be corrupt or missing
            }
        }
    }

    /// <summary>
    /// Clears all solutions from memory and deletes the database file.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _solutions.Clear();
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception)
            {
                // Ignore delete errors
            }
        }
    }

    /// <summary>
    /// Saves a solution to the database.
    /// </summary>
    /// <param name="cubeCode">The cube state code (key).</param>
    /// <param name="genome">The genome containing the solution moves.</param>
    public void SaveSolution(string cubeCode, TRubikGenome genome)
    {
        lock (_lock)
        {
            if (_solutions.ContainsKey(cubeCode)) return;

            var solution = new List<TMove>(genome.MovesCount);

            try
            {
                using var file = new FileStream(_filePath, FileMode.Append, FileAccess.Write);
                using var writer = new BinaryWriter(file);

                writer.Write(cubeCode);
                writer.Write(genome.MovesCount);

                for (int i = 0; i < genome.MovesCount; i++)
                {
                    var move = TMove.Decode((int)genome.Genes[i]);
                    writer.Write((byte)move.Axis);
                    writer.Write((byte)move.Slice);
                    writer.Write((byte)move.Plane);
                    writer.Write((byte)move.Angle);
                    solution.Add(move);
                }

                _solutions[cubeCode] = solution;
            }
            catch (Exception)
            {
                // Ignore write errors
            }
        }

        SolutionSaved?.Invoke(Count);
    }

    /// <summary>
    /// Tries all saved solutions against the current cube state.
    /// Returns the best improving moves found, or null if no improvement.
    /// </summary>
    /// <param name="cube">The cube to try solutions on.</param>
    /// <param name="currentScore">The current best score to beat.</param>
    /// <returns>Result with improving moves, or null if no improvement found.</returns>
    public TrySolutionsResult? TrySolutions(TRubikCube cube, double currentScore)
    {
        if (cube.ActiveCubie == null) return null;

        List<KeyValuePair<string, List<TMove>>> solutionsCopy;
        lock (_lock)
        {
            solutionsCopy = _solutions.ToList();
        }

        if (solutionsCopy.Count == 0) return null;

        var freeMoves = TRubikGenome.FreeMoves?.ToList();
        if (freeMoves == null || freeMoves.Count == 0) return null;

        double bestScore = currentScore;
        List<TMove>? bestMoves = null;

        foreach (var solution in solutionsCopy)
        {
            var decodedMoves = DecodeSolution(solution.Value, cube);
            if (decodedMoves.Count == 0) continue;

            // Try the solution directly and with each free move as prefix (conjugation)
            for (int j = -1; j < freeMoves.Count; j++)
            {
                var moves = new List<TMove>();

                if (j < 0)
                {
                    // Try solution directly
                    moves.AddRange(decodedMoves);
                }
                else
                {
                    // Try with conjugation: move + solution + inverse(move)
                    var prefixMove = TMove.Decode(freeMoves[j]);
                    moves.Add(prefixMove);
                    moves.AddRange(decodedMoves);

                    var inverseMove = TMove.Decode(freeMoves[j]);
                    inverseMove.Angle = 2 - inverseMove.Angle;
                    moves.Add(inverseMove);
                }

                // Test the moves on a copy
                var testCube = new TRubikCube(cube);
                foreach (var move in moves)
                    testCube.Turn(move);

                var score = testCube.Evaluate();
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMoves = moves;
                }
            }
        }

        if (bestMoves != null)
        {
            return new TrySolutionsResult
            {
                Moves = bestMoves,
                Fitness = bestScore
            };
        }

        return null;
    }

    /// <summary>
    /// Decodes a saved solution for the current cube's active cubie position.
    /// Solutions are position-independent, so we need to translate slice indices.
    /// </summary>
    private static List<TMove> DecodeSolution(List<TMove> solution, TRubikCube cube)
    {
        if (cube.ActiveCubie == null) return new List<TMove>();

        var pos = cube.ActiveCubie.Position;
        var map = new List<int>();
        var result = new List<TMove>();
        var sliceCountInCluster = TAffine.N;

        foreach (var origMove in solution)
        {
            if (!origMove.IsValid) continue;

            var move = new TMove
            {
                Axis = origMove.Axis,
                Slice = origMove.Slice,
                Plane = origMove.Plane,
                Angle = origMove.Angle
            };

            var idx = map.IndexOf(move.Slice);
            if (idx < 0)
            {
                idx = map.IndexOf(TRubikCube.Size - 1 - move.Slice);
                if (idx < 0)
                {
                    idx = map.Count;
                    map.Add(move.Slice);
                }
                else
                {
                    idx += sliceCountInCluster;
                }
            }

            if (idx < sliceCountInCluster)
                move.Slice = pos[idx];
            else
                move.Slice = TRubikCube.Size - 1 - pos[idx - sliceCountInCluster];

            result.Add(TMove.Decode(move.Encode()));
        }

        return result;
    }
}

/// <summary>
/// Result of trying saved solutions.
/// </summary>
public class TrySolutionsResult
{
    /// <summary>
    /// The moves that improved the score.
    /// </summary>
    public List<TMove> Moves { get; init; } = new();

    /// <summary>
    /// The fitness achieved after applying the moves.
    /// </summary>
    public double Fitness { get; init; }
}
