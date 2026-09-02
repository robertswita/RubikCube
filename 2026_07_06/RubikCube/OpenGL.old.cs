using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TGL
{
    public static unsafe class OpenGL
    {
        private static IntPtr OpenGlAsm;
        // Core OpenGL 1.1
        public static delegate* unmanaged[Stdcall]<int, void> Begin;
        public static delegate* unmanaged[Stdcall]<int, int, void> BindTexture;
        public static delegate* unmanaged[Stdcall]<int, int, void> BlendFunc;
        public static delegate* unmanaged[Stdcall]<int, void> Clear;
        public static delegate* unmanaged[Stdcall]<float, float, float, float, void> ClearColor;
        public static delegate* unmanaged[Stdcall]<byte, byte, byte, byte, void> Color4ub;
        public static delegate* unmanaged[Stdcall]<int, int*, void> DeleteTextures;
        public static delegate* unmanaged[Stdcall]<int, void> Disable;
        public static delegate* unmanaged[Stdcall]<int, int, int, void> DrawArrays;
        public static delegate* unmanaged[Stdcall]<int, void> Enable;
        public static delegate* unmanaged[Stdcall]<void> End;
        public static delegate* unmanaged[Stdcall]<int, int*, void> GenTextures;
        public static delegate* unmanaged[Stdcall]<int, float*, void> GetFloatv;
        public static delegate* unmanaged[Stdcall]<int> GetError;
        public static delegate* unmanaged[Stdcall]<float*, void> LoadMatrixf;
        public static delegate* unmanaged[Stdcall]<float*, void> MultMatrixf;
        public static delegate* unmanaged[Stdcall]<int, int, void> PolygonMode;
        public static delegate* unmanaged[Stdcall]<int, int, int, int, void> TexImage2D;
        public static delegate* unmanaged[Stdcall]<float, float, void> Vertex2f;
        public static delegate* unmanaged[Stdcall]<float, float, float, void> Vertex3f;
        public static delegate* unmanaged[Stdcall]<int, int, int, int, void> Viewport;
        // Extentions
        public static delegate* unmanaged[Cdecl]<int, void> ActiveTexture;
        public static delegate* unmanaged[Cdecl]<int, int, void> AttachShader;
        public static delegate* unmanaged[Cdecl]<int, int, void> BindBuffer;
        public static delegate* unmanaged[Cdecl]<int, int, void> BindFramebuffer;
        public static delegate* unmanaged[Cdecl]<int, int, void> BindRenderbuffer;
        public static delegate* unmanaged[Cdecl]<int, void> BindVertexArray;
        public static delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, int, void> BlitFramebuffer;
        public static delegate* unmanaged[Cdecl]<int, nint, void*, int, void> BufferData;
        public static delegate* unmanaged[Cdecl]<int, void> CompileShader;
        public static delegate* unmanaged[Cdecl]<int, int> CreateProgram;
        public static delegate* unmanaged[Cdecl]<int, int> CreateShader;
        public static delegate* unmanaged[Cdecl]<int, int, int, void> DispatchCompute;
        public static delegate* unmanaged[Cdecl]<int, void> EnableVertexAttribArray;
        public static delegate* unmanaged[Cdecl]<int, int, int, int, void> FramebufferRenderbuffer;
        public static delegate* unmanaged[Cdecl]<int, int*, void> GenBuffers;
        public static delegate* unmanaged[Cdecl]<int, int*, void> GenFramebuffers;
        public static delegate* unmanaged[Cdecl]<int, int*, void> GenRenderbuffers;
        public static delegate* unmanaged[Cdecl]<int, int*, void> GenVertexArrays;
        public static delegate* unmanaged[Cdecl]<int, void> GenerateMipmap;
        public static delegate* unmanaged[Cdecl]<int, int, int*, void> GetShaderiv;
        public static delegate* unmanaged[Cdecl]<int, void> LinkProgram;
        public static delegate* unmanaged[Cdecl]<MemoryBarrierFlags, void> MemoryBarrier;
        public static delegate* unmanaged[Cdecl]<int, int, int, int, int, void> RenderbufferStorageMultisample;
        private static delegate* unmanaged[Cdecl]<int, int, byte**, int*, void> ShaderSource;
        public static delegate* unmanaged[Cdecl]<int, float, void> Uniform1f;
        public static delegate* unmanaged[Cdecl]<int, int, void> Uniform1i;
        public static delegate* unmanaged[Cdecl]<int, uint, void> Uniform1ui;
        public static delegate* unmanaged[Cdecl]<int, int, byte, float*, void> UniformMatrix4fv;
        public static delegate* unmanaged[Cdecl]<int, void> UseProgram;
        public static delegate* unmanaged[Cdecl]<int, int, int, byte, int, void*, void> VertexAttribPointer;

        static OpenGL()
        {
            OpenGlAsm = NativeLibrary.Load("opengl32.dll");
            InitCore(out Begin);
            InitCore(out BindTexture);
            InitCore(out BlendFunc);
            InitCore(out Clear);
            InitCore(out ClearColor);
            InitCore(out Color4ub);
            InitCore(out DeleteTextures);
            InitCore(out Disable);
            InitCore(out DrawArrays);
            InitCore(out Enable);
            InitCore(out End);
            InitCore(out GenTextures);
            InitCore(out GetFloatv);
            InitCore(out GetError);
            InitCore(out LoadMatrixf);
            InitCore(out MultMatrixf);
            InitCore(out PolygonMode);
            InitCore(out TexImage2D);
            InitCore(out Vertex2f);
            InitCore(out Vertex3f);
            InitCore(out Viewport);
            InitExt(out ActiveTexture);
            InitExt(out AttachShader);
            InitExt(out BindBuffer);
            InitExt(out BindFramebuffer);
            InitExt(out BindRenderbuffer);
            InitExt(out BindVertexArray);
            InitExt(out BlitFramebuffer);
            InitExt(out BufferData);
            InitExt(out CompileShader);
            InitExt(out CreateProgram);
            InitExt(out CreateShader);
            InitExt(out DispatchCompute);
            InitExt(out EnableVertexAttribArray);
            InitExt(out FramebufferRenderbuffer);
            InitExt(out GenBuffers);
            InitExt(out GenFramebuffers);
            InitExt(out GenRenderbuffers);
            InitExt(out GenVertexArrays);
            InitExt(out GenerateMipmap);
            InitExt(out GetShaderiv);
            InitExt(out LinkProgram);
            InitExt(out MemoryBarrier);
            InitExt(out RenderbufferStorageMultisample);
            InitExt(out ShaderSource);
            InitExt(out Uniform1f);
            InitExt(out Uniform1i);
            InitExt(out Uniform1ui);
            InitExt(out UniformMatrix4fv);
            InitExt(out UseProgram);
            InitExt(out VertexAttribPointer);
        }

        /// <summary>
        /// Wysokopoziomowa i ultra-szybka metoda przesyłania kodu shadera bez alokacji GC.
        /// </summary>
        public static void ShaderSource(int shader, string source)
        {
            int length = source.Length;
            byte* pSourceBytes = stackalloc byte[length];
            System.Text.Encoding.ASCII.GetBytes(source, new Span<byte>(pSourceBytes, length));
            byte* pSourcePtr = pSourceBytes;
            ShaderSource(shader, 1, &pSourceBytes, &length);
        }

        /// <summary>
        /// Bezpieczne pobieranie logów kompilacji shadera przy użyciu wskaźników natywnych.
        /// </summary>
        public static string GetShaderInfoLog(int shader)
        {
            var glGetShaderInfoLogPtr = (delegate* unmanaged[Cdecl]<int, int, int*, byte*, void>)Win32.wglGetProcAddress("glGetShaderInfoLog");
            if (glGetShaderInfoLogPtr == null) return string.Empty;

            int logLength = 0;
            GetShaderiv(shader, 0x8B84, &logLength); // 0x8B84 = GL_INFO_LOG_LENGTH

            if (logLength <= 0) return string.Empty;

            byte* pLog = stackalloc byte[logLength];
            glGetShaderInfoLogPtr(shader, logLength, null, pLog);

            return Marshal.PtrToStringAnsi((IntPtr)pLog, logLength);
        }

        private static void InitCore<T>(out T field, [CallerArgumentExpression("field")] string expr = "") where T : unmanaged
        {
            string nativeName = "gl" + expr.Substring(4).Trim();
            IntPtr ptr = NativeLibrary.GetExport(OpenGlAsm, nativeName);
            field = *(T*)&ptr;
        }

        private static void InitExt<T>(out T field, [CallerArgumentExpression("field")] string expr = "") where T : unmanaged
        {
            string nativeName = "gl" + expr.Substring(4).Trim();
            IntPtr ptr = Win32.wglGetProcAddress(nativeName);
            if (ptr == IntPtr.Zero)
                throw new Exception($"OpenGL extension '{nativeName}' is not supported by your GPU driver.");
            field = *(T*)&ptr;
        }

        //  Constants
        public const int GL_TRIANGLES = 0x0004;
        public const int GL_QUADS = 0x0007;
        public const int GL_BLEND = 0x0BE2;
        public const int GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const int GL_ACCUM_BUFFER_BIT = 0x00000200;
        public const int GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const int GL_COLOR_BUFFER_BIT = 0x00004000;
        public const int GL_DEPTH_TEST = 0x0B71;
        public const int GL_MODELVIEW_MATRIX = 0x0BA6;
        public const int GL_TEXTURE_1D = 0x0DE0;
        public const int GL_TEXTURE_2D = 0x0DE1;
        public const int GL_TEXTURE0 = 0x84C0;
        public const int GL_BGR = 0x80E0;
        public const int GL_BGRA = 0x80E1;
        public const int GL_RGB8 = 0x8051;
        public const int GL_STATIC_DRAW = 0x88E4;
        public const int GL_DYNAMIC_DRAW = 0x88E8;
        public const int GL_DYNAMIC_COPY = 0x88EA;
        public const int GL_FRAGMENT_SHADER = 0x8B30;
        public const int GL_VERTEX_SHADER = 0x8B31;
        public const int GL_COMPUTE_SHADER = 0x91B9;
        public const int GL_COMPILE_STATUS = 0x8B81;
        public const int GL_INFO_LOG_LENGTH = 0x8B84;
        public const int GL_ARRAY_BUFFER = 0x8892;
        public const int GL_UNIFORM_BUFFER = 0x8A11;
        public const int GL_SHADER_STORAGE_BUFFER = 0x90D2;
        public const int GL_MULTISAMPLE = 0x809D;
        public const int GL_FRAMEBUFFER = 0x8D40;
        public const int GL_RENDERBUFFER = 0x8D41;
        public const int GL_READ_FRAMEBUFFER = 0x8CA8;
        public const int GL_DRAW_FRAMEBUFFER = 0x8CA9;
        public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
        public const int GL_DEPTH24_STENCIL8 = 0x88F0;
        //   DataType
        public const int GL_BYTE = 0x1400;
        public const int GL_UNSIGNED_BYTE = 0x1401;
        public const int GL_SHORT = 0x1402;
        public const int GL_UNSIGNED_SHORT = 0x1403;
        public const int GL_INT = 0x1404;
        public const int GL_FLOAT = 0x1406;
        //   PolygonMode
        public const int GL_POINT = 0x1B00;
        public const int GL_LINE = 0x1B01;
        public const int GL_FILL = 0x1B02;
        //   DrawBufferMode
        public const int GL_NONE = 0;
        public const int GL_FRONT_LEFT = 0x0400;
        public const int GL_FRONT_RIGHT = 0x0401;
        public const int GL_BACK_LEFT = 0x0402;
        public const int GL_BACK_RIGHT = 0x0403;
        public const int GL_FRONT = 0x0404;
        public const int GL_BACK = 0x0405;
        public const int GL_LEFT = 0x0406;
        public const int GL_RIGHT = 0x0407;
        public const int GL_FRONT_AND_BACK = 0x0408;
        public const int GL_AUX0 = 0x0409;
        public const int GL_AUX1 = 0x040A;
        public const int GL_AUX2 = 0x040B;
        public const int GL_AUX3 = 0x040C;
        //  BlendingFactorDest
        public const int GL_ZERO = 0;
        public const int GL_ONE = 1;
        public const int GL_SRC_COLOR = 0x0300;
        public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const int GL_SRC_ALPHA = 0x0302;
        public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const int GL_DST_ALPHA = 0x0304;
        public const int GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const int GL_POLYGON_SMOOTH = 0x0B41;
        //  Mag filters
        public const int GL_NEAREST = 0x2600;
        public const int GL_LINEAR = 0x2601;
        //  WGL Context
        public const int WGL_DRAW_TO_WINDOW = 0x2001;
        public const int WGL_DRAW_TO_BITMAP = 0x2002;
        public const int WGL_ACCELERATION = 0x2003;
        public const int WGL_SUPPORT_OPENGL = 0x2010;
        public const int WGL_DOUBLE_BUFFER = 0x2011;
        public const int WGL_STEREO = 0x2012;
        public const int WGL_PIXEL_TYPE = 0x2013;
        public const int WGL_COLOR_BITS = 0x2014;
        public const int WGL_DEPTH_BITS = 0x2022;
        public const int WGL_STENCIL_BITS = 0x2023;
        public const int WGL_FULL_ACCELERATION = 0x2027;
        public const int WGL_TYPE_RGBA = 0x202B;
        public const int WGL_SAMPLE_BUFFERS = 0x2041;
        public const int WGL_SAMPLES = 0x2042;

        [Flags]
        public enum MemoryBarrierFlags : uint
        {
            VertexAttribArray = 0x1,
            ElementArray = 0x2,
            Uniform = 0x4,
            TextureFetch = 0x8,
            ShaderImageAccess = 0x20,
            Command = 0x40,
            PixelBuffer = 0x80,
            TextureUpdate = 0x100,
            BufferUpdate = 0x200,
            Framebuffer = 0x400,
            TransformFeedback = 0x800,
            AtomicCounter = 0x1000,
            ShaderStorage = 0x2000,
            ClientMappedBuffer = 0x4000,
            QueryBuffer = 0x8000,
            All = 0xFFFFFFFF,
        }

    }
}
