/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej inteligencji
Updated for OpenTK 4.x
***********************************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TGL
{
    public class TGLContext
    {
        public TGLView View;
        public Rectangle Viewport;
        public TShape Root = new TShape();
        public TAffine Transform = new TAffine();

        // Shader program
        private int _shaderProgram;
        private int _vao;
        private int _vbo;
        private int _colorVbo;
        private bool _isInitialized;

        // Uniform locations
        private int _mvpLocation;

        // Vertex data lists (rebuilt each frame)
        private List<float> _vertices = new List<float>();
        private List<float> _colors = new List<float>();

        private const string VertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;

uniform mat4 uMVP;

out vec4 vColor;

void main()
{
    gl_Position = uMVP * vec4(aPosition, 1.0);
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

        public void Initialize()
        {
            if (_isInitialized) return;

            // Create and compile vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, VertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader, "Vertex");

            // Create and compile fragment shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, FragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader, "Fragment");

            // Create shader program
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vertexShader);
            GL.AttachShader(_shaderProgram, fragmentShader);
            GL.LinkProgram(_shaderProgram);

            // Check linking
            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_shaderProgram);
                throw new Exception($"Shader program linking failed: {infoLog}");
            }

            // Clean up shaders (they're linked into the program now)
            GL.DetachShader(_shaderProgram, vertexShader);
            GL.DetachShader(_shaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Get uniform locations
            _mvpLocation = GL.GetUniformLocation(_shaderProgram, "uMVP");

            // Create VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            // Create VBO for positions
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            // Create VBO for colors
            _colorVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _colorVbo);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            _isInitialized = true;
        }

        private void CheckShaderCompilation(int shader, string name)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"{name} shader compilation failed: {infoLog}");
            }
        }

        internal void DrawView()
        {
            Initialize();

            Viewport = View.ClientRectangle;
            var backColor = View.BackColor;

            GL.ClearColor(backColor.R / 255f, backColor.G / 255f, backColor.B / 255f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);

            Init();
            DrawScene();
        }

        void DrawScene()
        {
            // Clear vertex data
            _vertices.Clear();
            _colors.Clear();

            // Build vertex data from scene
            Transform = new TAffine();
            CollectVertices(Root);

            if (_vertices.Count == 0) return;

            // Upload vertex data to GPU
            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _colorVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _colors.Count * sizeof(float), _colors.ToArray(), BufferUsageHint.DynamicDraw);

            // Use shader program
            GL.UseProgram(_shaderProgram);

            // Set MVP matrix (simple orthographic projection for now)
            float aspect = (float)Viewport.Width / Viewport.Height;
            var projection = Matrix4.CreateOrthographic(4f * aspect, 4f, -100f, 100f);
            GL.UniformMatrix4(_mvpLocation, false, ref projection);

            // Draw
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertices.Count / 3);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        TObject3DComparer ZOrderComparer = new TObject3DComparer();

        protected void CollectVertices(TShape obj)
        {
            var transform = Transform.Clone();
            Transform = Transform * obj.Transform;
            obj.WorldTransform = Transform.Clone();

            var childrenList = new List<TShape>(obj.Children);
            if (obj.WorldTransform.Origin.Size > 2)
                childrenList.Sort(ZOrderComparer);

            for (int i = 0; i < obj.Children.Count; i++)
                CollectVertices(childrenList[i]);

            // Convert quads to triangles
            for (int i = 0; i < obj.Faces.Count; i += 4)
            {
                if (i + 3 >= obj.Faces.Count) break;

                var v0 = obj.Vertices[obj.Faces[i]];
                var v1 = obj.Vertices[obj.Faces[i + 1]];
                var v2 = obj.Vertices[obj.Faces[i + 2]];
                var v3 = obj.Vertices[obj.Faces[i + 3]];

                var color = obj.Colors[i / 4];
                float r = color.R / 255f;
                float g = color.G / 255f;
                float b = color.B / 255f;
                float a = obj.Transparency;

                // Transform vertices
                v0 = Transform * v0;
                v1 = Transform * v1;
                v2 = Transform * v2;
                v3 = Transform * v3;

                // Triangle 1: v0, v1, v2
                AddVertex(v0, r, g, b, a);
                AddVertex(v1, r, g, b, a);
                AddVertex(v2, r, g, b, a);

                // Triangle 2: v0, v2, v3
                AddVertex(v0, r, g, b, a);
                AddVertex(v2, r, g, b, a);
                AddVertex(v3, r, g, b, a);
            }

            Transform = transform;
        }

        private void AddVertex(TVector v, float r, float g, float b, float a)
        {
            _vertices.Add(v.X);
            _vertices.Add(v.Y);
            _vertices.Add(v.Size > 2 ? v.Z : 0f);

            _colors.Add(r);
            _colors.Add(g);
            _colors.Add(b);
            _colors.Add(a);
        }

        bool _IsTransparencyOn;
        public bool IsTransparencyOn
        {
            get { return _IsTransparencyOn; }
            set
            {
                _IsTransparencyOn = value;
                IsInited = false;
                View.Invalidate();
            }
        }

        public bool IsInited;

        void Init()
        {
            if (!IsInited)
            {
                GL.Enable(EnableCap.DepthTest);

                if (IsTransparencyOn)
                {
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                }
                else
                {
                    GL.Disable(EnableCap.Blend);
                }

                IsInited = true;
            }
        }
    }
}
