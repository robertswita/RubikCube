using System;
using System.Runtime.InteropServices;

namespace TGL
{
    public static unsafe class OpenGL
    {
        private static IntPtr OpenGlAsm;
        // Core OpenGL 1.1
        public static delegate* unmanaged[Stdcall]<uint, void> Begin;
        public static delegate* unmanaged[Stdcall]<uint, uint, void> BindTexture;
        public static delegate* unmanaged[Stdcall]<uint, uint, void> BlendFunc;
        public static delegate* unmanaged[Stdcall]<uint, void> Clear;
        public static delegate* unmanaged[Stdcall]<float, float, float, float, void> ClearColor;
        public static delegate* unmanaged[Stdcall]<byte, byte, byte, byte, void> Color4ub;
        public static delegate* unmanaged[Stdcall]<int, uint*, void> DeleteTextures;
        public static delegate* unmanaged[Stdcall]<uint, void> Disable;
        public static delegate* unmanaged[Stdcall]<uint, int, int, void> DrawArrays;
        public static delegate* unmanaged[Stdcall]<uint, void> Enable;
        public static delegate* unmanaged[Stdcall]<void> End;
        public static delegate* unmanaged[Stdcall]<int, uint*, void> GenTextures;
        public static delegate* unmanaged[Stdcall]<uint, float*, void> GetFloatv;
        public static delegate* unmanaged[Stdcall]<uint> GetError;
        public static delegate* unmanaged[Stdcall]<float*, void> LoadMatrixf;
        public static delegate* unmanaged[Stdcall]<float*, void> MultMatrixf;
        public static delegate* unmanaged[Stdcall]<uint, uint, void> PolygonMode;
        public static delegate* unmanaged[Stdcall]<uint, int, int, int, int, int, uint, uint, void*, void> TexImage2D;
        public static delegate* unmanaged[Stdcall]<float, float, void> Vertex2f;
        public static delegate* unmanaged[Stdcall]<float, float, float, void> Vertex3f;
        public static delegate* unmanaged[Stdcall]<int, int, int, int, void> Viewport;
        // Extentions
        public static delegate* unmanaged[Cdecl]<uint, void> ActiveTexture;
        public static delegate* unmanaged[Cdecl]<uint, uint, void> AttachShader;
        public static delegate* unmanaged[Cdecl]<uint, uint, void> BindBuffer;
        public static delegate* unmanaged[Cdecl]<uint, uint, uint, void> BindBufferBase;
        public static delegate* unmanaged[Cdecl]<uint, uint, void> BindFramebuffer;
        public static delegate* unmanaged[Cdecl]<uint, uint, void> BindRenderbuffer;
        public static delegate* unmanaged[Cdecl]<uint, void> BindVertexArray;
        public static delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, uint, uint, void> BlitFramebuffer;
        public static delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void> BufferData;
        public static delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void> BufferSubData;
        public static delegate* unmanaged[Cdecl]<uint, void> CompileShader;
        public static delegate* unmanaged[Cdecl]<uint> CreateProgram;
        public static delegate* unmanaged[Cdecl]<uint, uint> CreateShader;
        public static delegate* unmanaged[Cdecl]<uint, uint, uint, void> DispatchCompute;
        public static delegate* unmanaged[Cdecl]<uint, void> EnableVertexAttribArray;
        public static delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void> FramebufferRenderbuffer;
        public static delegate* unmanaged[Cdecl]<int, uint*, void> GenBuffers;
        public static delegate* unmanaged[Cdecl]<int, uint*, void> DeleteBuffers;
        public static delegate* unmanaged[Cdecl]<int, uint*, void> GenFramebuffers;
        public static delegate* unmanaged[Cdecl]<int, uint*, void> GenRenderbuffers;
        public static delegate* unmanaged[Cdecl]<int, uint*, void> GenVertexArrays;
        public static delegate* unmanaged[Cdecl]<uint, void> GenerateMipmap;
        public static delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void> GetBufferSubData;
        public static delegate* unmanaged[Cdecl]<uint, uint, int*, void> GetShaderiv;
        public static delegate* unmanaged[Cdecl]<uint, int, int*, uint*, void> GetAttachedShaders;
        public static delegate* unmanaged[Cdecl]<uint, void> LinkProgram;
        public static delegate* unmanaged[Cdecl]<MemoryBarrierFlags, void> MemoryBarrier;
        public static delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void> RenderbufferStorageMultisample;
        private static delegate* unmanaged[Cdecl]<uint, int, byte**, int*, void> glShaderSource;
        public static delegate* unmanaged[Cdecl]<int, float, void> Uniform1f;
        public static delegate* unmanaged[Cdecl]<int, int, void> Uniform1i;
        public static delegate* unmanaged[Cdecl]<int, uint, void> Uniform1ui;
        public static delegate* unmanaged[Cdecl]<int, int, byte, float*, void> UniformMatrix4fv;
        public static delegate* unmanaged[Cdecl]<uint, void> UseProgram;
        public static delegate* unmanaged[Cdecl]<uint, int, uint, byte, int, void*, void> VertexAttribPointer;

        private static void* GetCore(string csharpName)
        {
            return (void*)NativeLibrary.GetExport(OpenGlAsm, "gl" + csharpName);
        }
        private static void* GetExt(string csharpName)
        {
            string nativeName = "gl" + csharpName;
            IntPtr ptr = Win32.GetProcAddress(nativeName);
            if (ptr == IntPtr.Zero)
                throw new Exception($"OpenGL extension '{nativeName}' is not supported by your GPU.");
            return (void*)ptr;
        }
        static OpenGL()
        {
            OpenGlAsm = NativeLibrary.Load("opengl32.dll");
            Begin = (delegate* unmanaged[Stdcall]<uint, void>)GetCore("Begin");
            BindTexture = (delegate* unmanaged[Stdcall]<uint, uint, void>)GetCore("BindTexture");
            BlendFunc = (delegate* unmanaged[Stdcall]<uint, uint, void>)GetCore("BlendFunc");
            Clear = (delegate* unmanaged[Stdcall]<uint, void>)GetCore("Clear");
            ClearColor = (delegate* unmanaged[Stdcall]<float, float, float, float, void>)GetCore("ClearColor");
            Color4ub = (delegate* unmanaged[Stdcall]<byte, byte, byte, byte, void>)GetCore("Color4ub");
            DeleteTextures = (delegate* unmanaged[Stdcall]<int, uint*, void>)GetCore("DeleteTextures");
            Disable = (delegate* unmanaged[Stdcall]<uint, void>)GetCore("Disable");
            DrawArrays = (delegate* unmanaged[Stdcall]<uint, int, int, void>)GetCore("DrawArrays");
            Enable = (delegate* unmanaged[Stdcall]<uint, void>)GetCore("Enable");
            End = (delegate* unmanaged[Stdcall]<void>)GetCore("End");
            GenTextures = (delegate* unmanaged[Stdcall]<int, uint*, void>)GetCore("GenTextures");
            GetFloatv = (delegate* unmanaged[Stdcall]<uint, float*, void>)GetCore("GetFloatv");
            GetError = (delegate* unmanaged[Stdcall]<uint>)GetCore("GetError");
            LoadMatrixf = (delegate* unmanaged[Stdcall]<float*, void>)GetCore("LoadMatrixf");
            MultMatrixf = (delegate* unmanaged[Stdcall]<float*, void>)GetCore("MultMatrixf");
            PolygonMode = (delegate* unmanaged[Stdcall]<uint, uint, void>)GetCore("PolygonMode");
            TexImage2D = (delegate* unmanaged[Stdcall]<uint, int, int, int, int, int, uint, uint, void*, void>)GetCore("TexImage2D");
            Vertex2f = (delegate* unmanaged[Stdcall]<float, float, void>)GetCore("Vertex2f");
            Vertex3f = (delegate* unmanaged[Stdcall]<float, float, float, void>)GetCore("Vertex3f");
            Viewport = (delegate* unmanaged[Stdcall]<int, int, int, int, void>)GetCore("Viewport");
            ActiveTexture = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("ActiveTexture");
            AttachShader = (delegate* unmanaged[Cdecl]<uint, uint, void>)GetExt("AttachShader");
            BindBuffer = (delegate* unmanaged[Cdecl]<uint, uint, void>)GetExt("BindBuffer");
            BindBufferBase = (delegate* unmanaged[Cdecl]<uint, uint, uint, void>)GetExt("BindBufferBase");
            BindFramebuffer = (delegate* unmanaged[Cdecl]<uint, uint, void>)GetExt("BindFramebuffer");
            BindRenderbuffer = (delegate* unmanaged[Cdecl]<uint, uint, void>)GetExt("BindRenderbuffer");
            BindVertexArray = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("BindVertexArray");
            BlitFramebuffer = (delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, uint, uint, void>)GetExt("BlitFramebuffer");
            BufferData = (delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void>)GetExt("BufferData");
            BufferSubData = (delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)GetExt("BufferSubData");
            CompileShader = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("CompileShader");
            CreateProgram = (delegate* unmanaged[Cdecl]<uint>)GetExt("CreateProgram");
            CreateShader = (delegate* unmanaged[Cdecl]<uint, uint>)GetExt("CreateShader");
            DispatchCompute = (delegate* unmanaged[Cdecl]<uint, uint, uint, void>)GetExt("DispatchCompute");
            EnableVertexAttribArray = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("EnableVertexAttribArray");
            FramebufferRenderbuffer = (delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)GetExt("FramebufferRenderbuffer");
            GenBuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GetExt("GenBuffers");
            DeleteBuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GetExt("DeleteBuffers");
            GenFramebuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GetExt("GenFramebuffers");
            GenRenderbuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GetExt("GenRenderbuffers");
            GenVertexArrays = (delegate* unmanaged[Cdecl]<int, uint*, void>)GetExt("GenVertexArrays");
            GenerateMipmap = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("GenerateMipmap");
            GetBufferSubData = (delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)GetExt("GetBufferSubData");
            GetShaderiv = (delegate* unmanaged[Cdecl]<uint, uint, int*, void>)GetExt("GetShaderiv");
            GetAttachedShaders = (delegate* unmanaged[Cdecl]<uint, int, int*, uint*, void>)GetExt("GetAttachedShaders");
            LinkProgram = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("LinkProgram");
            MemoryBarrier = (delegate* unmanaged[Cdecl]<MemoryBarrierFlags, void>)GetExt("MemoryBarrier");
            RenderbufferStorageMultisample = (delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)GetExt("RenderbufferStorageMultisample");
            glShaderSource = (delegate* unmanaged[Cdecl]<uint, int, byte**, int*, void>)GetExt("ShaderSource");
            Uniform1f = (delegate* unmanaged[Cdecl]<int, float, void>)GetExt("Uniform1f");
            Uniform1i = (delegate* unmanaged[Cdecl]<int, int, void>)GetExt("Uniform1i");
            Uniform1ui = (delegate* unmanaged[Cdecl]<int, uint, void>)GetExt("Uniform1ui");
            UniformMatrix4fv = (delegate* unmanaged[Cdecl]<int, int, byte, float*, void>)GetExt("UniformMatrix4fv");
            UseProgram = (delegate* unmanaged[Cdecl]<uint, void>)GetExt("UseProgram");
            VertexAttribPointer = (delegate* unmanaged[Cdecl]<uint, int, uint, byte, int, void*, void>)GetExt("VertexAttribPointer");
        }

        public static void ShaderSource(uint shader, string source)
        {
            int length = source.Length;
            byte* pSourceBytes = stackalloc byte[length];
            System.Text.Encoding.ASCII.GetBytes(source, new Span<byte>(pSourceBytes, length));
            byte* pSourcePtr = pSourceBytes;
            glShaderSource(shader, 1, &pSourceBytes, &length);
        }

        public static string GetShaderInfoLog(uint shader)
        {
            var glGetShaderInfoLog = (delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)Win32.GetProcAddress("glGetShaderInfoLog");
            int logLength = 0;
            GetShaderiv(shader, 0x8B84, &logLength); // 0x8B84 = GL_INFO_LOG_LENGTH
            byte* pLog = stackalloc byte[logLength];
            glGetShaderInfoLog(shader, logLength, null, pLog);
            return Marshal.PtrToStringAnsi((IntPtr)pLog, logLength);
        }

        //  Constants
        public const uint GL_TRIANGLES = 0x0004;
        public const uint GL_QUADS = 0x0007;
        public const uint GL_BLEND = 0x0BE2;
        public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const uint GL_ACCUM_BUFFER_BIT = 0x00000200;
        public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        public const uint GL_DEPTH_TEST = 0x0B71;
        public const uint GL_MODELVIEW_MATRIX = 0x0BA6;
        public const uint GL_TEXTURE_1D = 0x0DE0;
        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_TEXTURE0 = 0x84C0;
        public const uint GL_BGR = 0x80E0;
        public const uint GL_BGRA = 0x80E1;
        public const uint GL_RGB8 = 0x8051;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_DYNAMIC_DRAW = 0x88E8;
        public const uint GL_DYNAMIC_COPY = 0x88EA;
        public const uint GL_FRAGMENT_SHADER = 0x8B30;
        public const uint GL_VERTEX_SHADER = 0x8B31;
        public const uint GL_COMPUTE_SHADER = 0x91B9;
        public const uint GL_COMPILE_STATUS = 0x8B81;
        public const uint GL_INFO_LOG_LENGTH = 0x8B84;
        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_UNIFORM_BUFFER = 0x8A11;
        public const uint GL_SHADER_STORAGE_BUFFER = 0x90D2;
        public const uint GL_MULTISAMPLE = 0x809D;
        public const uint GL_FRAMEBUFFER = 0x8D40;
        public const uint GL_RENDERBUFFER = 0x8D41;
        public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
        public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
        public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
        public const uint GL_DEPTH24_STENCIL8 = 0x88F0;
        //   DataType
        public const uint GL_BYTE = 0x1400;
        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_SHORT = 0x1402;
        public const uint GL_UNSIGNED_SHORT = 0x1403;
        public const uint GL_uint = 0x1404;
        public const uint GL_FLOAT = 0x1406;
        //   PolygonMode
        public const uint GL_POuint = 0x1B00;
        public const uint GL_LINE = 0x1B01;
        public const uint GL_FILL = 0x1B02;
        //   DrawBufferMode
        public const uint GL_NONE = 0;
        public const uint GL_FRONT_LEFT = 0x0400;
        public const uint GL_FRONT_RIGHT = 0x0401;
        public const uint GL_BACK_LEFT = 0x0402;
        public const uint GL_BACK_RIGHT = 0x0403;
        public const uint GL_FRONT = 0x0404;
        public const uint GL_BACK = 0x0405;
        public const uint GL_LEFT = 0x0406;
        public const uint GL_RIGHT = 0x0407;
        public const uint GL_FRONT_AND_BACK = 0x0408;
        public const uint GL_AUX0 = 0x0409;
        public const uint GL_AUX1 = 0x040A;
        public const uint GL_AUX2 = 0x040B;
        public const uint GL_AUX3 = 0x040C;
        //  BlendingFactorDest
        public const uint GL_ZERO = 0;
        public const uint GL_ONE = 1;
        public const uint GL_SRC_COLOR = 0x0300;
        public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const uint GL_SRC_ALPHA = 0x0302;
        public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const uint GL_DST_ALPHA = 0x0304;
        public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const uint GL_POLYGON_SMOOTH = 0x0B41;
        //  Mag filters
        public const uint GL_NEAREST = 0x2600;
        public const uint GL_LINEAR = 0x2601;
        //  WGL Context
        public const uint WGL_DRAW_TO_WINDOW = 0x2001;
        public const uint WGL_DRAW_TO_BITMAP = 0x2002;
        public const uint WGL_ACCELERATION = 0x2003;
        public const uint WGL_SUPPORT_OPENGL = 0x2010;
        public const uint WGL_DOUBLE_BUFFER = 0x2011;
        public const uint WGL_STEREO = 0x2012;
        public const uint WGL_PIXEL_TYPE = 0x2013;
        public const uint WGL_COLOR_BITS = 0x2014;
        public const uint WGL_DEPTH_BITS = 0x2022;
        public const uint WGL_STENCIL_BITS = 0x2023;
        public const uint WGL_FULL_ACCELERATION = 0x2027;
        public const uint WGL_TYPE_RGBA = 0x202B;
        public const uint WGL_SAMPLE_BUFFERS = 0x2041;
        public const uint WGL_SAMPLES = 0x2042;

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
