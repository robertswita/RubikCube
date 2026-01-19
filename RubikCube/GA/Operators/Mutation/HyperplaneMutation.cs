using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Hyperplane Mutation - transforms moves between different 3D "cells" in 4D+ cubes.
///
/// In an N-dimensional Rubik's cube:
/// - 3D cube has 6 faces (2D cells)
/// - 4D cube has 8 cells (3D cubes as "faces")
/// - 5D cube has 10 cells (4D hypercubes)
///
/// This mutation swaps a move to an analogous move operating on a different
/// hyperplane of the cube. It does this by:
/// 1. Shifting the axis by a random offset (mod N)
/// 2. Finding a corresponding rotation plane that maintains similar structure
///
/// For 3D cubes (N=3), this is equivalent to rotating the entire move coordinate
/// system - e.g., an X-axis rotation becomes a Y-axis rotation.
///
/// For 4D+ cubes, this explores symmetries between different hyperplanes,
/// which can help find solutions that work across the higher-dimensional structure.
///
/// Example (4D cube):
/// Original move: Axis=0, Slice=1, Plane=0 (XY rotation on slice 1 of X-axis)
/// After mutation with offset=2: Axis=2, Slice=1, Plane=? (need to find corresponding plane)
///
/// The mutation preserves:
/// - Slice position (same relative position in the new hyperplane)
/// - Angle (same rotation amount)
/// - Structural relationship between axis and plane where possible
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class HyperplaneMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        if (chromosome.Length == 0) return;

        // For 3D cubes, this mutation is less meaningful but still works
        // It becomes more powerful for 4D+ cubes
        int n = TAffine.N;
        if (n < 3) return;

        // Select random gene to mutate
        int idx = rng.Next(chromosome.Length);

        // Decode the move
        var move = TMove.Decode((int)chromosome.Genes[idx]);

        // Generate random axis offset (1 to N-1, so we always change)
        int axisOffset = rng.Next(1, n);
        int newAxis = (move.Axis + axisOffset) % n;

        // Get the original plane's axes
        int[] oldPlaneAxes = TAffine.Planes[move.Plane];

        // Try to find a corresponding plane for the new axis
        // Strategy: Find a plane that has similar "character" relative to the new axis
        int newPlane = FindCorrespondingPlane(move.Plane, move.Axis, newAxis, rng);

        // Create the mutated move
        var mutatedMove = new TMove
        {
            Axis = newAxis,
            Slice = move.Slice, // Preserve slice position
            Plane = newPlane,
            Angle = move.Angle  // Preserve angle
        };

        // Encode and store
        chromosome.Genes[idx] = mutatedMove.Encode();
    }

    /// <summary>
    /// Finds a rotation plane for the new axis that corresponds to the original plane.
    /// </summary>
    private static int FindCorrespondingPlane(int oldPlane, int oldAxis, int newAxis, Random rng)
    {
        int[] oldPlaneAxes = TAffine.Planes[oldPlane];
        int n = TAffine.N;

        // Determine the relationship between old plane and old axis
        bool planeContainsOldAxis = oldPlaneAxes[0] == oldAxis || oldPlaneAxes[1] == oldAxis;

        // Collect candidate planes based on relationship
        var candidates = new List<int>();

        for (int p = 0; p < TAffine.Planes.Length; p++)
        {
            int[] planeAxes = TAffine.Planes[p];
            bool planeContainsNewAxis = planeAxes[0] == newAxis || planeAxes[1] == newAxis;

            // Match the containment relationship
            if (planeContainsOldAxis == planeContainsNewAxis)
            {
                candidates.Add(p);
            }
        }

        // If we found matching candidates, pick one randomly
        if (candidates.Count > 0)
        {
            return candidates[rng.Next(candidates.Count)];
        }

        // Fallback: pick any valid plane
        return rng.Next(TAffine.Planes.Length);
    }
}
