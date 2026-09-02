/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej inteligencji
***********************************************************/
using GA;
using RubikCube;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
//using System.Xml;

namespace TGL
{
    public class TGLContext
    {
        public TGLView View;
        IntPtr HDC;
        IntPtr HRC;
        Win32.PIXELFORMATDESCRIPTOR pfd;
        public Rectangle Viewport;
        public TShape Root = new TShape();
        public TAffine Transform = new TAffine();
        //public int[] SsboCubies = new int[1];
        //public int[] SsboActiveCubies = new int[1];
        //public int[] SsboSolvedCubies = new int[1];
        //public int[] SsboWorkCubies = new int[1];
        //public int[] SsboPlanes = new int[1];
        //public int[] SsboPopulation = new int[1];
        public static int InitProgram;
        public static int EvaluateProgram;
        public static int SelCrossoverProgram;
        public static Ssbo PopulationBuffer;
        public static Ssbo ResultBuffer;
        public static int GenerationsCount;

        private static int ExtractDefineValue(string allText, string defineName)
        {
            string targetToken = "#define " + defineName;
            using var reader = new StringReader(allText);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith(targetToken, StringComparison.Ordinal))
                {
                    string valuePart = line.Substring(targetToken.Length).Trim();
                    if (int.TryParse(valuePart, out int result))
                        return result;
                }
            }
            throw new Exception("Could not find " + defineName + " in \"Setup.glsl.c\"");
        }
        public unsafe IntPtr Handle
        {
            get
            {
                if (HRC == IntPtr.Zero)
                {
                    HDC = View.CreateGraphics().GetHdc();
                    pfd = new Win32.PIXELFORMATDESCRIPTOR();
                    var idx = Win32.ChoosePixelFormat(HDC, pfd);
                    Win32.SetPixelFormat(HDC, idx, pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);

                    var setup = ReadManifestText("Resources.Setup.glsl.c");
                    InitProgram = OpenGL.CreateProgram();
                    var shader = CreateShader(OpenGL.GL_COMPUTE_SHADER, "Resources.Init.glsl.c", setup);
                    OpenGL.AttachShader(InitProgram, shader);
                    OpenGL.LinkProgram(InitProgram);
                    EvaluateProgram = OpenGL.CreateProgram();
                    shader = CreateShader(OpenGL.GL_COMPUTE_SHADER, "Resources.Evaluate.glsl.c", setup);
                    OpenGL.AttachShader(EvaluateProgram, shader);
                    OpenGL.LinkProgram(EvaluateProgram);
                    var populationCount = ExtractDefineValue(setup, "POPULATION_COUNT");
                    var genesCount = ExtractDefineValue(setup, "GENES_COUNT");
                    GenerationsCount = ExtractDefineValue(setup, "GENERATIONS_COUNT");
                    TChromosome.GenesLength = genesCount;
                    var specimenSize = (genesCount + 2) * sizeof(int);
                    var populationSize = populationCount * specimenSize;
                    PopulationBuffer = new Ssbo(populationSize);
                    OpenGL.BindBuffer(PopulationBuffer.Type, PopulationBuffer.Id);
                    OpenGL.BufferData(PopulationBuffer.Type, PopulationBuffer.Size, null, OpenGL.GL_DYNAMIC_COPY);
                    ResultBuffer = new Ssbo(specimenSize);
                    OpenGL.BindBuffer(ResultBuffer.Type, ResultBuffer.Id);
                    OpenGL.BufferData(ResultBuffer.Type, ResultBuffer.Size, null, OpenGL.GL_DYNAMIC_COPY);

                    ////OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_VERTEX_SHADER));
                    ////OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_FRAGMENT_SHADER));
                    //OpenGL.UseProgram(gpuProgram);
                    //OpenGL.GenBuffers(1, SsboCubies);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 0, SsboCubies[0]);
                    //OpenGL.GenBuffers(1, SsboActiveCubies);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 1, SsboActiveCubies[0]);
                    //OpenGL.GenBuffers(1, SsboSolvedCubies);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 2, SsboSolvedCubies[0]);
                    ////OpenGL.GenBuffers(1, SsboWorkCubies);
                    ////OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 3, SsboWorkCubies[0]);
                    //OpenGL.GenBuffers(1, SsboPlanes);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 4, SsboPlanes[0]);
                    //OpenGL.GenBuffers(1, SsboPopulation);
                    //OpenGL.BindBufferBase(OpenGL.GL_SHADER_STORAGE_BUFFER, 5, SsboPopulation[0]);
                }
                return HRC;
            }
        }

        static string ReadManifestText(string dotPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string rootNamespace = assembly.GetName().Name;
            string resourceName = $"{rootNamespace}.{dotPath}";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        int CreateShader(int shaderType, string dotPath, string setup = "")
        {
            var shader = OpenGL.CreateShader(shaderType);
            var code = ReadManifestText(dotPath);
            code.Replace("#include \"Setup.glsl.c\"", setup);
            OpenGL.ShaderSource(shader, code);
            OpenGL.CompileShader(shader);
            var status = new int[1];
            OpenGL.GetShader(shader, OpenGL.GL_COMPILE_STATUS, status);
            if (status[0] == 0)
            {
                var maxLength = new int[1];
                OpenGL.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, maxLength);
                var log = new StringBuilder(maxLength[0]);
                OpenGL.GetShaderInfoLog(shader, maxLength[0], IntPtr.Zero, log);
            }
            return shader;
        }


        internal void DrawView()
        {
            if (Handle != IntPtr.Zero)
            {
                Viewport = View.ClientRectangle;
                var backColor = View.BackColor;
                Win32.wglMakeCurrent(HDC, HRC);
                OpenGL.ClearColor(backColor.R / 255f, backColor.G / 255f, backColor.B / 255f, 1);
                OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                OpenGL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
                Init();
                //SetupCamera();
                DrawScene();
                Win32.SwapBuffers(HDC);
            }
        }

        //void SetupCamera()
        //{
        //    // Set up projection matrix
        //    OpenGL.glMatrixMode(OpenGL.GL_PROJECTION);
        //    OpenGL.glLoadIdentity();

        //    // Use perspective projection for better 3D viewing
        //    double aspect = Viewport.Width / (double)Viewport.Height;
        //    OpenGL.gluPerspective(45.0, aspect, 0.1, 100.0);

        //    // Position camera to view the scene
        //    OpenGL.gluLookAt(
        //        0, 0, 8,      // Camera position (looking from positive Z)
        //        0, 0, 0,      // Look at origin
        //        0, 1, 0       // Up vector
        //    );

        //    // Switch back to modelview matrix for object transformations
        //    OpenGL.glMatrixMode(OpenGL.GL_MODELVIEW);
        //}

        void DrawScene()
        {
            Transform = new TAffine();
            DrawObject(Root);
        }

        TObject3DComparer ZOrderComparer = new TObject3DComparer();
        protected void DrawObject(TShape obj)
        {
            var transform = Transform.Clone();
            Transform = Transform * obj.Transform;
            obj.WorldTransform = Transform.Clone();

            var childrenList = new List<TShape>(obj.Children);
            if (obj.WorldTransform.Origin.Size > 2)
                childrenList.Sort(ZOrderComparer);
            for (int i = 0; i < obj.Children.Count; i++)
                DrawObject(childrenList[i]);
            OpenGL.Begin(OpenGL.GL_QUADS);
            for (int i = 0; i < obj.Faces.Count; i++)
            {
                var v = obj.Vertices[obj.Faces[i]];
                if (i % 4 == 0)
                {
                    var color = obj.Colors[i / 4];
                    OpenGL.Color4ub(color.R, color.G, color.B, (byte)(255 * obj.Transparency));
                }
                v = Transform * v;
                if (v.Size == 2)
                    OpenGL.Vertex2f(v.X, v.Y);
                else
                    OpenGL.Vertex3f(v.X, v.Y, v.Z);
            }
            OpenGL.End();
            Transform = transform;
        }

        //void gluPickMatrix(double x, double y, double w, double h)
        //{
        //    /* Translate and scale the picked region to the entire window */
        //    //OpenGL.glTranslated((Viewport.Width - 2 * x) / w, (Viewport.Height - 2 * y) / h, 0);
        //    //OpenGL.glScaled(Viewport.Width / w, Viewport.Height / h, 1.0);
        //    OpenGL.glScaled(Viewport.Width / w, Viewport.Height / h, 1.0);
        //    OpenGL.glTranslated(1 - 2 * x / Viewport.Width, 1 - 2 * y / Viewport.Height, 0);
        //}

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
                OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                //OpenGL.glDisable(OpenGL.GL_CULL_FACE);  // Disable face culling to show all faces
                //OpenGL.glPolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                //OpenGL.glEnable(OpenGL.GL_TEXTURE_2D);
                //OpenGL.glEnable(OpenGL.GL_LIGHTING);
                //OpenGL.glEnable(OpenGL.GL_COLOR_MATERIAL);
                //OpenGL.glEnable(OpenGL.GL_NORMALIZE);
                if (IsTransparencyOn)
                {
                    OpenGL.glEnable(OpenGL.GL_BLEND);
                    OpenGL.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                }
                else
                    OpenGL.glDisable(OpenGL.GL_BLEND);
                IsInited = true;
            }
        }

    };

}
