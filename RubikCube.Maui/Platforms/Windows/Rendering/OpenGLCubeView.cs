#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Handlers;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using TGL;
using RubikCube.Maui.Rendering;
using RubikCube.Maui.Controls;

namespace RubikCube.Maui.Platforms.Windows.Rendering;

/// <summary>
/// MAUI view that renders the Rubik's Cube using OpenGL via Silk.NET.
/// Uses a custom handler to create the platform view with OpenGL context.
/// </summary>
public class OpenGLCubeView : Microsoft.Maui.Controls.View
{
    public TShape Root { get; set; } = new TShape();
    public bool IsTransparencyOn { get; set; }
    public new Color BackgroundColor { get; set; } = Colors.DarkSlateGray;
    public NativeCubeView? NativeViewParent { get; set; }

    private OpenGLCubeViewHandler? _handler;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _handler = Handler as OpenGLCubeViewHandler;
        _handler?.SetNativeViewParent(NativeViewParent);
        Invalidate();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _handler?.Resize((float)width, (float)height);
        Invalidate();
    }

    public void Invalidate()
    {
        _handler?.UpdateRenderState(Root, IsTransparencyOn, BackgroundColor);
    }
}

/// <summary>
/// Custom SwapChainPanel that hosts OpenGL rendering.
/// </summary>
class OpenGLPanel : SwapChainPanel
{
    public nint Hwnd { get; private set; }

    public OpenGLPanel()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get the HWND for this control
        try
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(
                Microsoft.Maui.MauiWinUIApplication.Current.Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window);
            Hwnd = windowHandle;
        }
        catch
        {
            Hwnd = IntPtr.Zero;
        }
    }
}

/// <summary>
/// Handler for OpenGLCubeView that creates the SwapChainPanel and manages OpenGL rendering.
/// </summary>
public class OpenGLCubeViewHandler : ViewHandler<OpenGLCubeView, SwapChainPanel>
{
    private GL? _gl;
    private nint _hwnd;
    private nint _hdc;
    private nint _hglrc;
    private OpenGLRenderer? _renderer;
    private NativeCubeView? _nativeViewParent;
    private DispatcherTimer? _renderTimer;

    // Render state
    private TShape _root = new TShape();
    private bool _isTransparencyOn;
    private float _bgR = 0.18f, _bgG = 0.31f, _bgB = 0.31f, _bgA = 1.0f;
    private bool _contextCreated;

    public static IPropertyMapper<OpenGLCubeView, OpenGLCubeViewHandler> PropertyMapper =
        new PropertyMapper<OpenGLCubeView, OpenGLCubeViewHandler>(ViewMapper);

    public OpenGLCubeViewHandler() : base(PropertyMapper) { }

    public void SetNativeViewParent(NativeCubeView? parent)
    {
        _nativeViewParent = parent;
    }

    protected override SwapChainPanel CreatePlatformView()
    {
        var panel = new OpenGLPanel
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
            // Ensure panel doesn't span across grid columns
            UseLayoutRounding = true
        };

        // Wait for panel to be loaded and initialized
        panel.Loaded += OnPanelLoaded;
        panel.SizeChanged += OnPlatformViewSizeChanged;

        return panel;
    }

    private void OnPlatformViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            Resize((float)e.NewSize.Width, (float)e.NewSize.Height);
        }
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is OpenGLPanel panel)
        {
            InitializeOpenGL(panel);
            StartRenderLoop();
        }
    }

    private void InitializeOpenGL(OpenGLPanel panel)
    {
        if (_contextCreated) return;

        try
        {
            _hwnd = panel.Hwnd;
            if (_hwnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get HWND for OpenGL panel");
                return;
            }

            _hdc = GetDC(_hwnd);
            if (_hdc == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get DC");
                return;
            }

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
            if (pixelFormat == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to choose pixel format");
                return;
            }

            if (!SetPixelFormat(_hdc, pixelFormat, ref pfd))
            {
                System.Diagnostics.Debug.WriteLine("Failed to set pixel format");
                return;
            }

            _hglrc = wglCreateContext(_hdc);
            if (_hglrc == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to create WGL context");
                return;
            }

            if (!wglMakeCurrent(_hdc, _hglrc))
            {
                System.Diagnostics.Debug.WriteLine("Failed to make WGL context current");
                return;
            }

            // Create Silk.NET GL context
            _gl = GL.GetApi(new WglContext(_hdc, _hglrc));

            // Create renderer
            _renderer = new OpenGLRenderer();
            _renderer.SetGL(_gl);

            var width = (float)Math.Max(panel.ActualWidth, 100);
            var height = (float)Math.Max(panel.ActualHeight, 100);
            _renderer.Initialize(width, height);
            _renderer.SetBackgroundColor(_bgR, _bgG, _bgB, _bgA);

            _contextCreated = true;
            System.Diagnostics.Debug.WriteLine($"OpenGL initialized successfully: {width}x{height}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGL initialization failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void StartRenderLoop()
    {
        if (_renderTimer != null) return;

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _renderTimer.Tick += (s, e) => Draw();
        _renderTimer.Start();
    }

    public void Resize(float width, float height)
    {
        if (_renderer != null && width > 0 && height > 0)
        {
            _renderer.Resize(width, height);
        }
    }

    public void UpdateRenderState(TShape root, bool isTransparencyOn, Color backgroundColor)
    {
        _root = root;
        _isTransparencyOn = isTransparencyOn;
        _bgR = backgroundColor.Red;
        _bgG = backgroundColor.Green;
        _bgB = backgroundColor.Blue;
        _bgA = backgroundColor.Alpha;

        if (_renderer != null)
        {
            _renderer.SetBackgroundColor(_bgR, _bgG, _bgB, _bgA);
        }
    }

    internal void Draw()
    {
        if (!_contextCreated || _renderer == null || _gl == null || _hdc == IntPtr.Zero || _hglrc == IntPtr.Zero)
            return;

        try
        {
            if (!wglMakeCurrent(_hdc, _hglrc))
                return;

            _renderer.Render(_root, _isTransparencyOn);
            SwapBuffers(_hdc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGL render failed: {ex.Message}");
        }
    }

    protected override void DisconnectHandler(SwapChainPanel platformView)
    {
        _renderTimer?.Stop();
        _renderTimer = null;

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

        if (platformView != null)
        {
            platformView.Loaded -= OnPanelLoaded;
            platformView.SizeChanged -= OnPlatformViewSizeChanged;
        }

        base.DisconnectHandler(platformView);
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
        public void SwapBuffers() => OpenGLCubeViewHandler.SwapBuffers(_hdc);
        public void MakeCurrent() => wglMakeCurrent(_hdc, _hglrc);
        public void Clear() => wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
        public void Dispose() { }
    }
}
#endif
