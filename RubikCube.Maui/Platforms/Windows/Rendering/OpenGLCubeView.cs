#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;
using TGL;
using RubikCube.Maui.Rendering;
using RubikCube.Maui.Controls;
using Microsoft.Maui.Controls;

namespace RubikCube.Maui.Platforms.Windows.Rendering;

/// <summary>
/// MAUI-compatible view that renders the Rubik's Cube using OpenGL via Silk.NET.
/// Uses a native HWND for OpenGL context.
/// </summary>
public class OpenGLCubeView : ContentView
{
    private OpenGLRenderer? _renderer;
    private GL? _gl;
    private nint _hwnd;
    private nint _hdc;
    private nint _hglrc;
    private bool _contextCreated;

    public TShape Root { get; set; } = new TShape();
    public bool IsTransparencyOn { get; set; }
    public new Color BackgroundColor { get; set; } = Colors.DarkSlateGray;
    public NativeCubeView? NativeViewParent { get; set; }

    public OpenGLCubeView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        InitializeOpenGL();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        CleanupOpenGL();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (_renderer != null && Width > 0 && Height > 0)
        {
            _renderer.Resize((float)Width, (float)Height);
            RequestRender();
        }
    }

    private void InitializeOpenGL()
    {
        if (_contextCreated) return;

        try
        {
            // Get the platform view's HWND
            var platformView = Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView == null) return;

            // For WinUI3, we need to get the HWND of the window
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(
                Microsoft.Maui.MauiWinUIApplication.Current.Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window);

            if (windowHandle == IntPtr.Zero) return;

            _hwnd = windowHandle;
            _hdc = GetDC(_hwnd);

            if (_hdc == IntPtr.Zero) return;

            // Set pixel format
            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
                iPixelType = PFD_TYPE_RGBA,
                cColorBits = 32,
                cDepthBits = 24,
                cStencilBits = 8,
                iLayerType = PFD_MAIN_PLANE
            };

            int pixelFormat = ChoosePixelFormat(_hdc, ref pfd);
            if (pixelFormat == 0) return;

            if (!SetPixelFormat(_hdc, pixelFormat, ref pfd)) return;

            _hglrc = wglCreateContext(_hdc);
            if (_hglrc == IntPtr.Zero) return;

            if (!wglMakeCurrent(_hdc, _hglrc)) return;

            // Create Silk.NET GL context
            _gl = GL.GetApi(new WglContext(_hdc, _hglrc));

            // Create renderer
            _renderer = new OpenGLRenderer();
            _renderer.SetGL(_gl);

            var width = (float)Math.Max(Width, 100);
            var height = (float)Math.Max(Height, 100);
            _renderer.Initialize(width, height);
            _renderer.SetBackgroundColor(
                BackgroundColor.Red,
                BackgroundColor.Green,
                BackgroundColor.Blue,
                BackgroundColor.Alpha);

            _contextCreated = true;
            RequestRender();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGL initialization failed: {ex.Message}");
        }
    }

    private void CleanupOpenGL()
    {
        _renderer?.Dispose();
        _renderer = null;

        if (_hglrc != IntPtr.Zero)
        {
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(_hglrc);
            _hglrc = IntPtr.Zero;
        }

        if (_hdc != IntPtr.Zero && _hwnd != IntPtr.Zero)
        {
            ReleaseDC(_hwnd, _hdc);
            _hdc = IntPtr.Zero;
        }

        _contextCreated = false;
    }

    public void Invalidate()
    {
        RequestRender();
    }

    private void RequestRender()
    {
        if (!_contextCreated || _renderer == null || _gl == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (!wglMakeCurrent(_hdc, _hglrc)) return;

                _renderer.Render(Root, IsTransparencyOn);
                SwapBuffers(_hdc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenGL render failed: {ex.Message}");
            }
        });
    }

    #region Win32 P/Invoke

    private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    private const uint PFD_SUPPORT_OPENGL = 0x00000020;
    private const uint PFD_DOUBLEBUFFER = 0x00000001;
    private const byte PFD_TYPE_RGBA = 0;
    private const byte PFD_MAIN_PLANE = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct PIXELFORMATDESCRIPTOR
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
        public byte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;
    }

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    private static extern int ChoosePixelFormat(nint hdc, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll")]
    private static extern bool SetPixelFormat(nint hdc, int format, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll")]
    private static extern bool SwapBuffers(nint hdc);

    [DllImport("opengl32.dll")]
    private static extern nint wglCreateContext(nint hdc);

    [DllImport("opengl32.dll")]
    private static extern bool wglMakeCurrent(nint hdc, nint hglrc);

    [DllImport("opengl32.dll")]
    private static extern bool wglDeleteContext(nint hglrc);

    [DllImport("opengl32.dll")]
    private static extern nint wglGetProcAddress(string lpszProc);

    #endregion

    /// <summary>
    /// Silk.NET GL context wrapper for WGL.
    /// </summary>
    private class WglContext : IGLContext
    {
        private readonly nint _hdc;
        private readonly nint _hglrc;

        public WglContext(nint hdc, nint hglrc)
        {
            _hdc = hdc;
            _hglrc = hglrc;
        }

        public nint Handle => _hglrc;
        public IGLContextSource? Source => null;
        public bool IsCurrent => true;

        public nint GetProcAddress(string proc, int? slot = null)
        {
            nint addr = wglGetProcAddress(proc);
            if (addr == IntPtr.Zero)
            {
                // Try getting from opengl32.dll directly
                addr = NativeLibrary.TryLoad("opengl32.dll", out var lib)
                    ? NativeLibrary.GetExport(lib, proc)
                    : IntPtr.Zero;
            }
            return addr;
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = GetProcAddress(proc, slot);
            return addr != IntPtr.Zero;
        }

        public void SwapInterval(int interval) { }
        public void SwapBuffers() => OpenGLCubeView.SwapBuffers(_hdc);
        public void MakeCurrent() => wglMakeCurrent(_hdc, _hglrc);
        public void Clear() => wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
        public void Dispose() { }
    }
}
#endif
