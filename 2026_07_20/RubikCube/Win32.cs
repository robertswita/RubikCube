using System;
using System.Runtime.InteropServices;

namespace TGL
{
    public static unsafe class Win32
    {
        private static IntPtr OpenGlAsm;
        private static IntPtr GdiAsm;
        // ========================================================
        // FUNKCJE GDI32 (Konwencja: Stdcall)
        // ========================================================
        public static delegate* unmanaged[Stdcall]<IntPtr, PIXELFORMATDESCRIPTOR*, int> ChoosePixelFormat;
        public static delegate* unmanaged[Stdcall]<IntPtr, int, int, PIXELFORMATDESCRIPTOR*, int> DescribePixelFormat;
        public static delegate* unmanaged[Stdcall]<IntPtr, int, PIXELFORMATDESCRIPTOR*, int> SetPixelFormat;
        public static delegate* unmanaged[Stdcall]<IntPtr, int> SwapBuffers;

        // ========================================================
        // FUNKCJE WGL / OPENGL32 CORE (Konwencja: Stdcall)
        // ========================================================
        public static delegate* unmanaged[Stdcall]<IntPtr, IntPtr> wglCreateContext;
        public static delegate* unmanaged[Stdcall]<IntPtr, int> wglDeleteContext;
        public static delegate* unmanaged[Stdcall]<IntPtr> wglGetCurrentContext;
        public static delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> wglMakeCurrent;
        public static delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> wglShareLists;
        public static delegate* unmanaged[Stdcall]<IntPtr, uint, uint, uint, int> wglUseFontBitmaps;
        private static delegate* unmanaged[Stdcall]<byte*, IntPtr> wglGetProcAddress;

        private static void* GetGdi(string name)
        { return (void*)NativeLibrary.GetExport(GdiAsm, name); }

        private static void* GetGl(string name)
        { return (void*)NativeLibrary.GetExport(OpenGlAsm, name); }

        public static IntPtr GetProcAddress(string name)
        {
            int length = name.Length;
            byte* pNameBytes = stackalloc byte[length + 1];
            System.Text.Encoding.ASCII.GetBytes(name, new Span<byte>(pNameBytes, length));
            pNameBytes[length] = 0;
            return wglGetProcAddress(pNameBytes);
        }

        static Win32()
        {
            // Funkcje z gdi32.dll
            GdiAsm = NativeLibrary.Load("gdi32.dll");
            ChoosePixelFormat = (delegate* unmanaged[Stdcall]<IntPtr, PIXELFORMATDESCRIPTOR*, int>)GetGdi("ChoosePixelFormat");
            DescribePixelFormat = (delegate* unmanaged[Stdcall]<IntPtr, int, int, PIXELFORMATDESCRIPTOR*, int>)GetGdi("DescribePixelFormat");
            SetPixelFormat = (delegate* unmanaged[Stdcall]<IntPtr, int, PIXELFORMATDESCRIPTOR*, int>)GetGdi("SetPixelFormat");
            SwapBuffers = (delegate* unmanaged[Stdcall]<IntPtr, int>)GetGdi("SwapBuffers");
            // Funkcje z opengl32.dll
            OpenGlAsm = NativeLibrary.Load("opengl32.dll");
            wglCreateContext = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr>)GetGl("wglCreateContext");
            wglDeleteContext = (delegate* unmanaged[Stdcall]<IntPtr, int>)GetGl("wglDeleteContext");
            wglGetCurrentContext = (delegate* unmanaged[Stdcall]<IntPtr>)GetGl("wglGetCurrentContext");
            wglMakeCurrent = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)GetGl("wglMakeCurrent");
            wglShareLists = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)GetGl("wglShareLists");
            wglUseFontBitmaps = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, uint, int>)GetGl("wglUseFontBitmapsW");
            wglGetProcAddress = (delegate* unmanaged[Stdcall]<byte*, IntPtr>)GetGl("wglGetProcAddress");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public sbyte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;

            public static PIXELFORMATDESCRIPTOR CreateDefault()
            {
                PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
                pfd.nSize = (ushort)sizeof(PIXELFORMATDESCRIPTOR);
                pfd.nVersion = 1;
                pfd.cColorBits = 32;
                pfd.cDepthBits = 24;
                pfd.cStencilBits = 8;
                pfd.iPixelType = PFD_TYPE_RGBA;
                pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
                return pfd;
            }
        }

        public const uint CS_VREDRAW = 0x0001;
        public const uint CS_HREDRAW = 0x0002;
        public const uint CS_DBLCLKS = 0x0008;
        public const uint CS_OWNDC = 0x0020;
        public const uint CS_CLASSDC = 0x0040;
        public const uint CS_PARENTDC = 0x0080;
        public const uint CS_NOCLOSE = 0x0200;
        public const uint CS_SAVEBITS = 0x0800;
        public const uint CS_BYTEALIGNCLIENT = 0x1000;
        public const uint CS_BYTEALIGNWINDOW = 0x2000;
        public const uint CS_GLOBALCLASS = 0x4000;

        public const uint PFD_DOUBLEBUFFER = 1;
        public const uint PFD_STEREO = 2;
        public const uint PFD_DRAW_TO_WINDOW = 4;
        public const uint PFD_DRAW_TO_BITMAP = 8;
        public const uint PFD_SUPPORT_GDI = 16;
        public const uint PFD_SUPPORT_OPENGL = 32;
        public const uint PFD_GENERIC_FORMAT = 64;
        public const uint PFD_NEED_PALETTE = 128;
        public const uint PFD_NEED_SYSTEM_PALETTE = 256;
        public const uint PFD_SWAP_EXCHANGE = 512;
        public const uint PFD_SWAP_COPY = 1024;
        public const uint PFD_SWAP_LAYER_BUFFERS = 2048;
        public const uint PFD_GENERIC_ACCELERATED = 4096;
        public const uint PFD_SUPPORT_DIRECTDRAW = 8192;

        public const sbyte PFD_MAIN_PLANE = 0;
        public const sbyte PFD_OVERLAY_PLANE = 1;
        public const sbyte PFD_UNDERLAY_PLANE = -1;

        public const byte PFD_TYPE_RGBA = 0;
        public const byte PFD_TYPE_COLORINDEX = 1;
    }
}
