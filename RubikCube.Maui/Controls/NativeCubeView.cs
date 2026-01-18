using TGL;
using Microsoft.Maui.Controls;

namespace RubikCube.Maui.Controls;

/// <summary>
/// Event args for scroll wheel events.
/// </summary>
public class ScrollWheelEventArgs : EventArgs
{
    public float DeltaX { get; }
    public float DeltaY { get; }

    public ScrollWheelEventArgs(float deltaX, float deltaY)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
    }
}

/// <summary>
/// Event args for mouse drag events (used for rotation on Windows).
/// </summary>
public class MouseDragEventArgs : EventArgs
{
    public float DeltaX { get; }
    public float DeltaY { get; }
    public bool IsStarting { get; }
    public bool IsEnding { get; }

    public MouseDragEventArgs(float deltaX, float deltaY, bool isStarting = false, bool isEnding = false)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
        IsStarting = isStarting;
        IsEnding = isEnding;
    }
}

/// <summary>
/// Cross-platform wrapper for native GPU-based cube rendering.
/// Uses Metal on macOS and OpenGL on Windows.
/// Falls back to SkiaSharp on other platforms.
/// </summary>
public class NativeCubeView : ContentView
{
    private View? _platformView;
    private bool _isInitialized;

    public TShape Root { get; set; } = new TShape();
    public bool IsTransparencyOn { get; set; }
    public new Color BackgroundColor { get; set; } = Colors.DarkSlateGray;

    /// <summary>
    /// Event raised when the scroll wheel is used over the view.
    /// </summary>
    public event EventHandler<ScrollWheelEventArgs>? ScrollWheelChanged;

    /// <summary>
    /// Event raised when mouse is dragged over the view (Windows only).
    /// </summary>
    public event EventHandler<MouseDragEventArgs>? MouseDragChanged;

    public NativeCubeView()
    {
        // Use Loaded event to ensure window is ready before OpenGL initialization
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        InitializePlatformView();
    }

    internal void RaiseScrollWheelChanged(float deltaX, float deltaY)
    {
        ScrollWheelChanged?.Invoke(this, new ScrollWheelEventArgs(deltaX, deltaY));
    }

    internal void RaiseMouseDragChanged(float deltaX, float deltaY, bool isStarting = false, bool isEnding = false)
    {
        MouseDragChanged?.Invoke(this, new MouseDragEventArgs(deltaX, deltaY, isStarting, isEnding));
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _platformView = null;
        _isInitialized = false;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void InitializePlatformView()
    {
        if (_isInitialized) return;

#if MACCATALYST
        var metalView = new Platforms.MacCatalyst.Rendering.MetalCubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            BackgroundColor = BackgroundColor,
            NativeViewParent = this
        };
        _platformView = metalView;
        this.Content = metalView;
#elif WINDOWS
        var openGLView = new Platforms.Windows.Rendering.OpenGLCubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            ClearColor = BackgroundColor,
            NativeViewParent = this
        };
        _platformView = openGLView;
        this.Content = openGLView;
#else
        // Fallback to the SkiaSharp-based CubeView
        var skiaView = new CubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            BackgroundColor = BackgroundColor
        };
        _platformView = skiaView;
        this.Content = skiaView;
#endif

        _isInitialized = true;
    }

    public void Invalidate()
    {
        if (_platformView == null) return;

#if MACCATALYST
        if (_platformView is Platforms.MacCatalyst.Rendering.MetalCubeView metalView)
        {
            metalView.Root = Root;
            metalView.IsTransparencyOn = IsTransparencyOn;
            metalView.BackgroundColor = BackgroundColor;
            metalView.Invalidate();
        }
#elif WINDOWS
        if (_platformView is Platforms.Windows.Rendering.OpenGLCubeView openGLView)
        {
            openGLView.Root = Root;
            openGLView.IsTransparencyOn = IsTransparencyOn;
            openGLView.ClearColor = BackgroundColor;
            openGLView.Invalidate();
        }
#else
        if (_platformView is CubeView skiaView)
        {
            skiaView.Root = Root;
            skiaView.IsTransparencyOn = IsTransparencyOn;
            skiaView.BackgroundColor = BackgroundColor;
            skiaView.Invalidate();
        }
#endif
    }
}
