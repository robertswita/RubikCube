using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Orthogonal Conjugation Mutation - creates conjugations using moves on orthogonal planes.
///
/// In N-dimensional space, two rotation planes are orthogonal if they share no common axes.
/// This is only possible when N >= 4:
///
/// 3D (3 planes): No orthogonal pairs exist
///   - (0,1), (0,2), (1,2) - every pair shares one axis
///
/// 4D (6 planes): 3 orthogonal pairs exist
///   - (0,1) ⊥ (2,3) - XY orthogonal to ZW
///   - (0,2) ⊥ (1,3) - XZ orthogonal to YW
///   - (0,3) ⊥ (1,2) - XW orthogonal to YZ
///
/// 5D (10 planes): More orthogonal pairs exist
///
/// Conjugations with orthogonal planes have special properties:
/// - They affect fewer pieces than arbitrary conjugations
/// - The moves "commute better" - less interference between A and B
/// - Creates more "geometrically pure" transformations
///
/// Algorithm:
/// 1. Select a random move A from the chromosome
/// 2. Find an orthogonal plane to A's rotation plane (if one exists)
/// 3. Generate move B on the orthogonal plane
/// 4. Insert the pattern A, B, A', B' (commutator) at that position
///
/// For 3D cubes where no orthogonal planes exist, falls back to using
/// planes that share minimal axes (most distant planes).
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class OrthogonalConjugationMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cache of orthogonal plane pairs, built once per dimension configuration
    private static int _cachedN = -1;
    private static List<(int plane1, int plane2)>? _orthogonalPairs;

    public void Mutate(T chromosome, Random rng)
    {
        if (chromosome.Length < 4) return; // Need space for A, B, A', B'

        int n = TAffine.N;

        // Build orthogonal pairs cache if needed
        if (_cachedN != n)
        {
            BuildOrthogonalPairsCache(n);
        }

        // Select random position for the commutator pattern
        int pos = rng.Next(chromosome.Length - 3);

        // Get move A from chromosome
        var moveA = TMove.Decode((int)chromosome.Genes[pos]);

        // Find orthogonal (or most distant) plane for move B
        int planeB = FindOrthogonalPlane(moveA.Plane, rng);

        // Generate move B on the orthogonal plane
        var moveB = new TMove
        {
            Axis = SelectAxisForPlane(planeB, rng),
            Slice = rng.Next(TRubikCube.Size),
            Plane = planeB,
            Angle = rng.Next(3)
        };

        // Create inverted moves
        var moveAInv = new TMove
        {
            Axis = moveA.Axis,
            Slice = moveA.Slice,
            Plane = moveA.Plane,
            Angle = (2 - moveA.Angle + 3) % 3 // Invert: 0->2, 1->1, 2->0
        };

        var moveBInv = new TMove
        {
            Axis = moveB.Axis,
            Slice = moveB.Slice,
            Plane = moveB.Plane,
            Angle = (2 - moveB.Angle + 3) % 3
        };

        // Insert commutator pattern: A, B, A', B'
        chromosome.Genes[pos] = moveA.Encode();
        chromosome.Genes[pos + 1] = moveB.Encode();
        chromosome.Genes[pos + 2] = moveAInv.Encode();
        chromosome.Genes[pos + 3] = moveBInv.Encode();
    }

    private static void BuildOrthogonalPairsCache(int n)
    {
        _cachedN = n;
        _orthogonalPairs = new List<(int, int)>();

        int numPlanes = TAffine.Planes.Length;

        for (int p1 = 0; p1 < numPlanes; p1++)
        {
            for (int p2 = p1 + 1; p2 < numPlanes; p2++)
            {
                if (AreOrthogonal(p1, p2))
                {
                    _orthogonalPairs.Add((p1, p2));
                }
            }
        }
    }

    /// <summary>
    /// Two planes are orthogonal if they share no common axes.
    /// </summary>
    private static bool AreOrthogonal(int plane1, int plane2)
    {
        int[] axes1 = TAffine.Planes[plane1];
        int[] axes2 = TAffine.Planes[plane2];

        // Check if they share any axis
        return axes1[0] != axes2[0] && axes1[0] != axes2[1] &&
               axes1[1] != axes2[0] && axes1[1] != axes2[1];
    }

    /// <summary>
    /// Find an orthogonal plane for the given plane.
    /// Falls back to most distant plane if no orthogonal exists.
    /// </summary>
    private static int FindOrthogonalPlane(int plane, Random rng)
    {
        // Look for orthogonal pairs involving this plane
        var orthogonalOptions = new List<int>();

        if (_orthogonalPairs != null)
        {
            foreach (var pair in _orthogonalPairs)
            {
                if (pair.plane1 == plane)
                    orthogonalOptions.Add(pair.plane2);
                else if (pair.plane2 == plane)
                    orthogonalOptions.Add(pair.plane1);
            }
        }

        // If orthogonal planes exist, pick one randomly
        if (orthogonalOptions.Count > 0)
        {
            return orthogonalOptions[rng.Next(orthogonalOptions.Count)];
        }

        // Fallback: find plane that shares fewest axes (most distant)
        return FindMostDistantPlane(plane, rng);
    }

    /// <summary>
    /// Find a plane that shares the minimum number of axes with the given plane.
    /// Used as fallback for 3D cubes where no orthogonal planes exist.
    /// </summary>
    private static int FindMostDistantPlane(int plane, Random rng)
    {
        int[] planeAxes = TAffine.Planes[plane];
        int numPlanes = TAffine.Planes.Length;

        var candidates = new List<int>();
        int minShared = int.MaxValue;

        for (int p = 0; p < numPlanes; p++)
        {
            if (p == plane) continue;

            int[] otherAxes = TAffine.Planes[p];
            int shared = 0;

            if (planeAxes[0] == otherAxes[0] || planeAxes[0] == otherAxes[1]) shared++;
            if (planeAxes[1] == otherAxes[0] || planeAxes[1] == otherAxes[1]) shared++;

            if (shared < minShared)
            {
                minShared = shared;
                candidates.Clear();
                candidates.Add(p);
            }
            else if (shared == minShared)
            {
                candidates.Add(p);
            }
        }

        return candidates.Count > 0 ? candidates[rng.Next(candidates.Count)] : (plane + 1) % numPlanes;
    }

    /// <summary>
    /// Select an appropriate axis for a rotation in the given plane.
    /// The axis should not be one of the plane's axes (perpendicular to the plane).
    /// </summary>
    private static int SelectAxisForPlane(int plane, Random rng)
    {
        int[] planeAxes = TAffine.Planes[plane];
        int n = TAffine.N;

        // Find axes not in the plane
        var perpendicularAxes = new List<int>();
        for (int axis = 0; axis < n; axis++)
        {
            if (axis != planeAxes[0] && axis != planeAxes[1])
            {
                perpendicularAxes.Add(axis);
            }
        }

        if (perpendicularAxes.Count > 0)
        {
            return perpendicularAxes[rng.Next(perpendicularAxes.Count)];
        }

        // Fallback (shouldn't happen in valid geometry)
        return rng.Next(n);
    }
}
