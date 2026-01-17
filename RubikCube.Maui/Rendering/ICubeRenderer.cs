using TGL;

namespace RubikCube.Maui.Rendering;

/// <summary>
/// Platform-agnostic interface for GPU-based cube rendering.
/// Implementations: MetalRenderer (macOS), OpenGLRenderer (Windows)
/// </summary>
public interface ICubeRenderer : IDisposable
{
    /// <summary>
    /// Initialize the renderer with the given viewport dimensions.
    /// </summary>
    void Initialize(float width, float height);

    /// <summary>
    /// Handle viewport resize.
    /// </summary>
    void Resize(float width, float height);

    /// <summary>
    /// Render the scene with the given root shape.
    /// </summary>
    void Render(TShape root, bool isTransparencyOn);

    /// <summary>
    /// Set the background color for rendering.
    /// </summary>
    void SetBackgroundColor(float r, float g, float b, float a);
}

/// <summary>
/// Vertex data structure for GPU rendering.
/// </summary>
public struct CubeVertex
{
    public float X, Y, Z;      // Position
    public float R, G, B, A;   // Color with alpha

    public CubeVertex(float x, float y, float z, float r, float g, float b, float a)
    {
        X = x; Y = y; Z = z;
        R = r; G = g; B = b; A = a;
    }

    public static int SizeInBytes => sizeof(float) * 7;
}
