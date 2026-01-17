using TGL;

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

    public NativeCubeView()
    {
        // Initialize platform view immediately, not on Loaded
        // This prevents layout issues on Windows
        InitializePlatformView();

        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    internal void RaiseScrollWheelChanged(float deltaX, float deltaY)
    {
        ScrollWheelChanged?.Invoke(this, new ScrollWheelEventArgs(deltaX, deltaY));
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
        Content = metalView;
#elif WINDOWS
        var openGLView = new Platforms.Windows.Rendering.OpenGLCubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            BackgroundColor = BackgroundColor,
            NativeViewParent = this
        };
        _platformView = openGLView;
        Content = openGLView;
#else
        // Fallback to the SkiaSharp-based CubeView
        var skiaView = new CubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            BackgroundColor = BackgroundColor
        };
        _platformView = skiaView;
        Content = skiaView;
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
            openGLView.BackgroundColor = BackgroundColor;
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
