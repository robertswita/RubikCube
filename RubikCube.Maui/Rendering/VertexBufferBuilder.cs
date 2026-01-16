using TGL;

namespace RubikCube.Maui.Rendering;

/// <summary>
/// Builds vertex buffer data from TShape hierarchy.
/// Traverses the shape tree, applies transforms, and produces GPU-ready vertex data.
/// </summary>
public class VertexBufferBuilder
{
    private readonly List<CubeVertex> _vertices = new();
    private readonly List<float> _faceDepths = new();
    private readonly TObject3DComparer _zOrderComparer = new();

    private bool _isTransparencyOn;

    /// <summary>
    /// Build vertex data from the shape hierarchy.
    /// When isTransparencyOn is true, uses object transparency values and sorts by depth.
    /// When false, all faces are fully opaque.
    /// </summary>
    public CubeVertex[] BuildVertices(TShape root, float scale, bool isTransparencyOn)
    {
        _vertices.Clear();
        _faceDepths.Clear();
        _isTransparencyOn = isTransparencyOn;

        var transform = new TAffine();
        CollectVertices(root, transform, scale);

        if (isTransparencyOn)
        {
            return SortFacesByDepth();
        }

        return _vertices.ToArray();
    }

    private void CollectVertices(TShape obj, TAffine parentTransform, float scale)
    {
        var transform = parentTransform * obj.Transform;
        obj.WorldTransform = transform.Clone();

        // Sort children by Z for proper draw order (back to front)
        var childrenList = new List<TShape>(obj.Children);
        if (obj.WorldTransform.Origin.Size > 2)
            childrenList.Sort(_zOrderComparer);

        // Recursively process children
        foreach (var child in childrenList)
            CollectVertices(child, transform, scale);

        // Process this object's faces
        if (obj.Faces.Count > 0 && obj.Vertices.Count > 0)
        {
            for (int i = 0; i < obj.Faces.Count; i += 4)
            {
                if (i + 3 >= obj.Faces.Count) break;

                var colorIndex = i / 4;
                var color = colorIndex < obj.Colors.Count ? obj.Colors[colorIndex] :
#if MAUI
                    Microsoft.Maui.Graphics.Colors.White;
#else
                    System.Drawing.Color.White;
#endif

                // Transform vertices
                var v0 = transform * obj.Vertices[obj.Faces[i]];
                var v1 = transform * obj.Vertices[obj.Faces[i + 1]];
                var v2 = transform * obj.Vertices[obj.Faces[i + 2]];
                var v3 = transform * obj.Vertices[obj.Faces[i + 3]];

                // Calculate average Z for this face (for depth sorting)
                float avgZ = (v0.Z + v1.Z + v2.Z + v3.Z) / 4;
                _faceDepths.Add(avgZ);

                // Apply scale and add vertices (as two triangles for the quad)
                float r, g, b, a;
#if MAUI
                r = color.Red;
                g = color.Green;
                b = color.Blue;
                a = _isTransparencyOn ? obj.Transparency : 1.0f;
#else
                r = color.R / 255f;
                g = color.G / 255f;
                b = color.B / 255f;
                a = _isTransparencyOn ? obj.Transparency : 1.0f;
#endif

                // Triangle 1: v0, v1, v2
                _vertices.Add(new CubeVertex(v0.X * scale, v0.Y * scale, v0.Z * scale, r, g, b, a));
                _vertices.Add(new CubeVertex(v1.X * scale, v1.Y * scale, v1.Z * scale, r, g, b, a));
                _vertices.Add(new CubeVertex(v2.X * scale, v2.Y * scale, v2.Z * scale, r, g, b, a));

                // Triangle 2: v0, v2, v3
                _vertices.Add(new CubeVertex(v0.X * scale, v0.Y * scale, v0.Z * scale, r, g, b, a));
                _vertices.Add(new CubeVertex(v2.X * scale, v2.Y * scale, v2.Z * scale, r, g, b, a));
                _vertices.Add(new CubeVertex(v3.X * scale, v3.Y * scale, v3.Z * scale, r, g, b, a));
            }
        }
    }

    private CubeVertex[] SortFacesByDepth()
    {
        // Each face has 6 vertices (2 triangles)
        int faceCount = _faceDepths.Count;
        var indices = new int[faceCount];
        for (int i = 0; i < faceCount; i++)
            indices[i] = i;

        // Sort by depth (back to front for proper transparency)
        Array.Sort(indices, (a, b) => _faceDepths[a].CompareTo(_faceDepths[b]));

        var sortedVertices = new CubeVertex[_vertices.Count];
        for (int i = 0; i < faceCount; i++)
        {
            int srcOffset = indices[i] * 6;
            int dstOffset = i * 6;
            for (int j = 0; j < 6; j++)
            {
                sortedVertices[dstOffset + j] = _vertices[srcOffset + j];
            }
        }

        return sortedVertices;
    }
}
