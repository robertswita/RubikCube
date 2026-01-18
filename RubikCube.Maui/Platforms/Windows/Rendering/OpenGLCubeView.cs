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
    public Color ClearColor { get; set; } = Colors.DarkSlateGray;
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
        _handler?.UpdateRenderState(Root, IsTransparencyOn, ClearColor);
    }
}

/// <summary>
/// Custom SwapChainPanel that hosts OpenGL rendering via an overlay HWND.
/// WinUI 3 windows use DirectX composition, so we need a separate top-level window for WGL.
/// </summary>
class OpenGLPanel : SwapChainPanel
{
    public nint Hwnd { get; private set; }
    public nint OverlayHwnd { get; private set; }
    private const string OverlayClassName = "RubikCubeOpenGLOverlay";
    private static bool _classRegistered;

    public event Action<float, float>? ScrollWheelChanged;
    public event Action<int>? OverlaySizeChanged;

    public OpenGLPanel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var delta = point.Properties.MouseWheelDelta;
        // MouseWheelDelta is typically 120 per notch, normalize it
        float normalizedDelta = delta / 120f * 10f;
        ScrollWheelChanged?.Invoke(0, normalizedDelta);
        e.Handled = true;
    }

    private Microsoft.UI.Xaml.Window? _parentWindow;
    private Microsoft.UI.Windowing.AppWindow? _appWindow;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _parentWindow = Microsoft.Maui.MauiWinUIApplication.Current.Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_parentWindow);
            Hwnd = windowHandle;

            // Get AppWindow to track position changes
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (_appWindow != null)
            {
                _appWindow.Changed += OnAppWindowChanged;
            }

            // Register window class for overlay window (only once)
            if (!_classRegistered)
            {
                var wc = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    style = CS_OWNDC,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                    hInstance = GetModuleHandle(null),
                    hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                    lpszClassName = OverlayClassName
                };
                RegisterClassEx(ref wc);
                _classRegistered = true;
            }

            // Get initial position in screen coordinates
            GetWindowRect(Hwnd, out RECT parentRect);

            // Create overlay window as a popup (not child) - separate top-level window
            OverlayHwnd = CreateWindowEx(
                0,
                OverlayClassName,
                "OpenGL",
                WS_POPUP | WS_VISIBLE,
                parentRect.Left + 400, parentRect.Top + 32,  // Initial position
                (int)Math.Max(ActualWidth, 100), (int)Math.Max(ActualHeight, 100),
                Hwnd,  // Owner (not parent)
                IntPtr.Zero,
                GetModuleHandle(null),
                IntPtr.Zero);

            // Trigger initial positioning
            UpdateOverlayPosition();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create overlay window: {ex.Message}");
            Hwnd = IntPtr.Zero;
            OverlayHwnd = IntPtr.Zero;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_appWindow != null)
        {
            _appWindow.Changed -= OnAppWindowChanged;
            _appWindow = null;
        }

        if (OverlayHwnd != IntPtr.Zero)
        {
            DestroyWindow(OverlayHwnd);
            OverlayHwnd = IntPtr.Zero;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateOverlayPosition();
    }

    private void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        // Update overlay position when window moves or resizes
        if (args.DidPositionChange || args.DidSizeChange)
        {
            UpdateOverlayPosition();
        }
    }

    public void UpdateOverlayPosition()
    {
        if (OverlayHwnd == IntPtr.Zero || Hwnd == IntPtr.Zero) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        try
        {
            var window = Microsoft.Maui.MauiWinUIApplication.Current.Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window?.Content is FrameworkElement content)
            {
                // Get panel position relative to window content
                var transform = this.TransformToVisual(content);
                var point = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));

                // Convert client coordinates (0,0) to screen coordinates to find where content starts
                POINT clientOrigin = new POINT { X = 0, Y = 0 };
                ClientToScreen(Hwnd, ref clientOrigin);

                // Keep the view square - use the smaller dimension
                int size = (int)Math.Min(ActualWidth, ActualHeight);

                // Center the square view within the available space
                int offsetX = (int)((ActualWidth - size) / 2);
                int offsetY = (int)((ActualHeight - size) / 2);

                // Calculate screen position using client area origin
                int screenX = clientOrigin.X + (int)point.X + offsetX;
                int screenY = clientOrigin.Y + (int)point.Y + offsetY;

                SetWindowPos(OverlayHwnd, IntPtr.Zero,
                    screenX, screenY,
                    size, size,
                    SWP_NOZORDER | SWP_NOACTIVATE);

                // Notify the handler about the new square size
                OverlaySizeChanged?.Invoke(size);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to reposition overlay: {ex.Message}");
        }
    }

    // P/Invoke constants
    private const uint CS_OWNDC = 0x0020;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const nint IDC_ARROW = 32512;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_MOUSEWHEEL = 0x020A;

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
    private static readonly WndProcDelegate _wndProcDelegate = WndProc;

    private static bool _isMouseDown;
    private static int _lastMouseX, _lastMouseY;
    private static float _pendingWheelDelta;
    private static float _pendingDragDeltaX, _pendingDragDeltaY;
    private static bool _pendingDragStart, _pendingDragEnd;

    private static nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_MOUSEWHEEL:
                // Extract wheel delta from wParam (high word)
                int delta = (short)((wParam >> 16) & 0xFFFF);
                _pendingWheelDelta += delta / 120f * 10f;
                break;

            case WM_LBUTTONDOWN:
                _isMouseDown = true;
                _lastMouseX = (short)(lParam & 0xFFFF);
                _lastMouseY = (short)((lParam >> 16) & 0xFFFF);
                _pendingDragStart = true;
                SetCapture(hWnd);
                break;

            case WM_LBUTTONUP:
                _isMouseDown = false;
                _pendingDragEnd = true;
                ReleaseCapture();
                break;

            case WM_MOUSEMOVE:
                if (_isMouseDown)
                {
                    int x = (short)(lParam & 0xFFFF);
                    int y = (short)((lParam >> 16) & 0xFFFF);
                    _pendingDragDeltaX += x - _lastMouseX;
                    _pendingDragDeltaY += y - _lastMouseY;
                    _lastMouseX = x;
                    _lastMouseY = y;
                }
                break;
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public static float ConsumePendingWheelDelta()
    {
        var delta = _pendingWheelDelta;
        _pendingWheelDelta = 0;
        return delta;
    }

    public static (float deltaX, float deltaY, bool isStarting, bool isEnding) ConsumePendingDrag()
    {
        var result = (_pendingDragDeltaX, _pendingDragDeltaY, _pendingDragStart, _pendingDragEnd);
        _pendingDragDeltaX = 0;
        _pendingDragDeltaY = 0;
        _pendingDragStart = false;
        _pendingDragEnd = false;
        return result;
    }

    [DllImport("user32.dll")]
    private static extern nint SetCapture(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint LoadCursor(nint hInstance, nint lpCursorName);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}

/// <summary>
/// Handler for OpenGLCubeView that creates the SwapChainPanel and manages OpenGL rendering.
/// </summary>
public class OpenGLCubeViewHandler : ViewHandler<OpenGLCubeView, SwapChainPanel>
{
    // Static constructor to load opengl32.dll before any WGL calls
    static OpenGLCubeViewHandler()
    {
        LoadLibrary("opengl32.dll");
    }

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
        new PropertyMapper<OpenGLCubeView, OpenGLCubeViewHandler>(ViewMapper)
        {
            // Remove BackgroundColor mapping as SwapChainPanel doesn't support it
            [nameof(IView.Background)] = MapBackground
        };

    public OpenGLCubeViewHandler() : base(PropertyMapper) { }

    private static void MapBackground(OpenGLCubeViewHandler handler, OpenGLCubeView view)
    {
        // SwapChainPanel doesn't support Background property - ignore it
        // The background color is handled internally by OpenGL clear color
    }

    public void SetNativeViewParent(NativeCubeView? parent)
    {
        _nativeViewParent = parent;
    }

    protected override SwapChainPanel CreatePlatformView()
    {
        var panel = new OpenGLPanel
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch
        };

        panel.Loaded += OnPanelLoaded;
        panel.SizeChanged += OnPlatformViewSizeChanged;
        panel.ScrollWheelChanged += OnScrollWheelChanged;
        panel.OverlaySizeChanged += OnOverlaySizeChanged;

        return panel;
    }

    private void OnScrollWheelChanged(float deltaX, float deltaY)
    {
        _nativeViewParent?.RaiseScrollWheelChanged(deltaX, deltaY);
    }

    private void OnOverlaySizeChanged(int size)
    {
        if (_renderer != null && size > 0)
        {
            _renderer.Resize(size, size);
        }
    }

    private void OnPlatformViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Don't call Resize directly - let UpdateOverlayPosition handle it
        // with the square size calculation via OnOverlaySizeChanged
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is OpenGLPanel panel)
        {
            InitializeOpenGL(panel);
            StartRenderLoop();

            // Force update overlay position now that renderer is ready
            panel.UpdateOverlayPosition();
        }
    }

    private void InitializeOpenGL(OpenGLPanel panel)
    {
        if (_contextCreated) return;

        try
        {
            _hwnd = panel.OverlayHwnd;
            if (_hwnd == IntPtr.Zero) return;

            _hdc = GetDC(_hwnd);
            if (_hdc == IntPtr.Zero) return;

            // Set pixel format to match master branch configuration
            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER | PFD_STEREO,
                cAlphaBits = 32
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

            var width = (float)Math.Max(panel.ActualWidth, 100);
            var height = (float)Math.Max(panel.ActualHeight, 100);
            _renderer.Initialize(width, height);
            _renderer.SetBackgroundColor(_bgR, _bgG, _bgB, _bgA);

            _contextCreated = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGL initialization failed: {ex.Message}");
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
            // No offset needed - the child window is positioned correctly
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
            // Check for pending wheel delta from the overlay window
            var wheelDelta = OpenGLPanel.ConsumePendingWheelDelta();
            if (wheelDelta != 0)
            {
                _nativeViewParent?.RaiseScrollWheelChanged(0, wheelDelta);
            }

            // Check for pending drag events from the overlay window
            var (dragDeltaX, dragDeltaY, isStarting, isEnding) = OpenGLPanel.ConsumePendingDrag();
            if (isStarting || isEnding || dragDeltaX != 0 || dragDeltaY != 0)
            {
                _nativeViewParent?.RaiseMouseDragChanged(dragDeltaX, dragDeltaY, isStarting, isEnding);
            }

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

    private const uint PFD_DOUBLEBUFFER = 0x00000001;
    private const uint PFD_STEREO = 0x00000002;
    private const uint PFD_SUPPORT_GDI = 0x00000010;
    private const uint PFD_SUPPORT_OPENGL = 0x00000020;

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
    private static extern int DescribePixelFormat(nint hdc, int iPixelFormat, uint nBytes, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll")]
    private static extern bool SwapBuffers(nint hdc);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadLibrary(string lpLibFileName);

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
