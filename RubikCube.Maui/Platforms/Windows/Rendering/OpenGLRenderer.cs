#if WINDOWS
using Silk.NET.OpenGL;
using TGL;
using RubikCube.Maui.Rendering;

namespace RubikCube.Maui.Platforms.Windows.Rendering;

/// <summary>
/// OpenGL-based renderer for Windows using Silk.NET.
/// </summary>
public class OpenGLRenderer : ICubeRenderer
{
    private GL? _gl;
    private uint _vao;
    private uint _vbo;
    private uint _shaderProgram;

    private readonly VertexBufferBuilder _bufferBuilder = new();
    private float _width, _height;
    private float _bgR = 0.18f, _bgG = 0.31f, _bgB = 0.31f, _bgA = 1.0f;

    private bool _isInitialized;

    private const string VertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;

out vec4 vColor;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
    vColor = aColor;
}
";

    private const string FragmentShaderSource = @"
#version 330 core
in vec4 vColor;
out vec4 FragColor;

void main()
{
    FragColor = vColor;
}
";

    public void SetGL(GL gl)
    {
        _gl = gl;
    }

    public void Initialize(float width, float height)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("GL context must be set before initialization");
        }

        _width = width;
        _height = height;

        // Create VAO
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        // Create VBO
        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        // Compile shaders
        _shaderProgram = CreateShaderProgram();

        // Set up vertex attributes
        // Position (3 floats)
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)CubeVertex.SizeInBytes, 0);

        // Color (4 floats)
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)CubeVertex.SizeInBytes, 3 * sizeof(float));

        // Enable depth testing
        _gl.Enable(EnableCap.DepthTest);

        _isInitialized = true;
    }

    private uint CreateShaderProgram()
    {
        if (_gl == null) return 0;

        // Compile vertex shader
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VertexShaderSource);
        _gl.CompileShader(vertexShader);

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Vertex shader compilation failed: {infoLog}");
        }

        // Compile fragment shader
        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Fragment shader compilation failed: {infoLog}");
        }

        // Link program
        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int pStatus);
        if (pStatus == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }

        // Cleanup
        _gl.DetachShader(program, vertexShader);
        _gl.DetachShader(program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        return program;
    }

    public void Resize(float width, float height)
    {
        _width = width;
        _height = height;

        if (_gl != null && _isInitialized)
        {
            _gl.Viewport(0, 0, (uint)width, (uint)height);
        }
    }

    public void SetBackgroundColor(float r, float g, float b, float a)
    {
        _bgR = r;
        _bgG = g;
        _bgB = b;
        _bgA = a;
    }

    public void Render(TShape root, bool isTransparencyOn)
    {
        if (!_isInitialized || _gl == null) return;

        // Clear
        _gl.ClearColor(_bgR, _bgG, _bgB, _bgA);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Build vertex data
        float scale = 0.4f;
        var vertices = _bufferBuilder.BuildVertices(root, scale, isTransparencyOn);

        if (vertices.Length == 0) return;

        // Enable blending for transparency
        if (isTransparencyOn)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
        }

        // Update vertex buffer
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        unsafe
        {
            fixed (CubeVertex* ptr = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * CubeVertex.SizeInBytes),
                    ptr, BufferUsageARB.DynamicDraw);
            }
        }

        // Draw
        _gl.UseProgram(_shaderProgram);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)vertices.Length);
    }

    public void Dispose()
    {
        if (_gl != null && _isInitialized)
        {
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_shaderProgram);
        }

        _isInitialized = false;
    }
}
#endif
