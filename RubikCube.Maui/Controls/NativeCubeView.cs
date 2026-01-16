using TGL;

namespace RubikCube.Maui.Controls;

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

    public NativeCubeView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        InitializePlatformView();
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
            BackgroundColor = BackgroundColor
        };
        _platformView = metalView;
        Content = metalView;
#elif WINDOWS
        var openGLView = new Platforms.Windows.Rendering.OpenGLCubeView
        {
            Root = Root,
            IsTransparencyOn = IsTransparencyOn,
            BackgroundColor = BackgroundColor
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
