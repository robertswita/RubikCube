#if MACCATALYST
using Metal;
using MetalKit;
using UIKit;
using CoreGraphics;
using Foundation;
using TGL;
using RubikCube.Maui.Rendering;
using RubikCube.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace RubikCube.Maui.Platforms.MacCatalyst.Rendering;

/// <summary>
/// MAUI view that renders the Rubik's Cube using Metal.
/// Uses a custom handler to create the MTKView as the platform view.
/// </summary>
public class MetalCubeView : Microsoft.Maui.Controls.View
{
    public TShape Root { get; set; } = new TShape();
    public bool IsTransparencyOn { get; set; }
    public new Color BackgroundColor { get; set; } = Colors.DarkSlateGray;
    public NativeCubeView? NativeViewParent { get; set; }

    private MetalCubeViewHandler? _handler;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _handler = Handler as MetalCubeViewHandler;
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
/// Custom MTKView that captures scroll wheel events via UIPanGestureRecognizer.
/// </summary>
class ScrollableMTKView : MTKView
{
    public event Action<float, float>? ScrollWheelChanged;
    private UIPanGestureRecognizer? _scrollGesture;
    private CGPoint _lastTranslation;

    public ScrollableMTKView(CGRect frame, IMTLDevice device) : base(frame, device)
    {
        // Create a pan gesture recognizer configured to handle scroll wheel/trackpad
        _scrollGesture = new UIPanGestureRecognizer(HandleScrollGesture);
        _scrollGesture.AllowedScrollTypesMask = UIScrollTypeMask.All;
        _scrollGesture.MaximumNumberOfTouches = 0; // No touch, only scroll wheel/trackpad
        AddGestureRecognizer(_scrollGesture);
    }

    private void HandleScrollGesture(UIPanGestureRecognizer gesture)
    {
        var translation = gesture.TranslationInView(this);

        if (gesture.State == UIGestureRecognizerState.Began)
        {
            _lastTranslation = CGPoint.Empty;
        }

        // Calculate delta from last position
        float deltaX = (float)(translation.X - _lastTranslation.X);
        float deltaY = (float)(translation.Y - _lastTranslation.Y);
        _lastTranslation = translation;

        if (gesture.State == UIGestureRecognizerState.Changed)
        {
            ScrollWheelChanged?.Invoke(deltaX, deltaY);
        }

        if (gesture.State == UIGestureRecognizerState.Ended ||
            gesture.State == UIGestureRecognizerState.Cancelled)
        {
            _lastTranslation = CGPoint.Empty;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _scrollGesture != null)
        {
            RemoveGestureRecognizer(_scrollGesture);
            _scrollGesture.Dispose();
            _scrollGesture = null;
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Handler for MetalCubeView that creates the MTKView and manages Metal rendering.
/// Uses MTKViewDelegate for proper render cycle integration.
/// </summary>
public class MetalCubeViewHandler : ViewHandler<MetalCubeView, MTKView>
{
    private IMTLDevice? _device;
    private IMTLCommandQueue? _commandQueue;
    private IMTLRenderPipelineState? _pipelineState;
    private IMTLDepthStencilState? _depthStencilStateOpaque;
    private IMTLDepthStencilState? _depthStencilStateTransparent;
    private IMTLBuffer? _vertexBuffer;
    private readonly VertexBufferBuilder _bufferBuilder = new();
    private MetalViewDelegate? _delegate;
    private NativeCubeView? _nativeViewParent;

    // Render state - updated by UpdateRenderState, read during Draw
    private TShape _root = new TShape();
    private bool _isTransparencyOn;
    private float _bgR = 0.18f, _bgG = 0.31f, _bgB = 0.31f, _bgA = 1.0f;
    private int _vertexCount;

    private const string ShaderSource = @"
#include <metal_stdlib>
using namespace metal;

struct VertexIn {
    float3 position [[attribute(0)]];
    float4 color [[attribute(1)]];
};

struct VertexOut {
    float4 position [[position]];
    float4 color;
};

vertex VertexOut vertex_main(VertexIn in [[stage_in]]) {
    VertexOut out;
    // Metal NDC: X,Y in [-1,1], Z in [0,1]
    // Map Z from [-1,1] to [0,1] to avoid clipping
    float z = (in.position.z + 1.0) * 0.5;
    out.position = float4(in.position.xy, z, 1.0);
    out.color = in.color;
    return out;
}

fragment float4 fragment_main(VertexOut in [[stage_in]]) {
    return in.color;
}
";

    public static IPropertyMapper<MetalCubeView, MetalCubeViewHandler> PropertyMapper =
        new PropertyMapper<MetalCubeView, MetalCubeViewHandler>(ViewMapper);

    public MetalCubeViewHandler() : base(PropertyMapper) { }

    public void SetNativeViewParent(NativeCubeView? parent)
    {
        _nativeViewParent = parent;
    }

    protected override MTKView CreatePlatformView()
    {
        _device = MTLDevice.SystemDefault;
        if (_device == null)
        {
            throw new Exception("Metal is not supported on this device");
        }

        _commandQueue = _device.CreateCommandQueue();

        var view = new ScrollableMTKView(CGRect.Empty, _device)
        {
            ColorPixelFormat = MTLPixelFormat.BGRA8Unorm,
            DepthStencilPixelFormat = MTLPixelFormat.Depth32Float,
            ClearColor = new MTLClearColor(_bgR, _bgG, _bgB, _bgA),
            // Use continuous rendering for smooth updates
            Paused = false,
            EnableSetNeedsDisplay = false,
            PreferredFramesPerSecond = 60
        };

        // Subscribe to scroll wheel events
        view.ScrollWheelChanged += OnScrollWheelChanged;

        CreatePipeline();
        CreateDepthStencilState();

        // Set up delegate for rendering
        _delegate = new MetalViewDelegate(this);
        view.Delegate = _delegate;

        return view;
    }

    private void OnScrollWheelChanged(float deltaX, float deltaY)
    {
        _nativeViewParent?.RaiseScrollWheelChanged(deltaX, deltaY);
    }

    private void CreatePipeline()
    {
        if (_device == null) return;

        Foundation.NSError error;
        var library = _device.CreateLibrary(ShaderSource, new MTLCompileOptions(), out error);
        if (library == null || error != null!)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create shader library: {error?.LocalizedDescription}");
            return;
        }

        var vertexFunction = library.CreateFunction("vertex_main");
        var fragmentFunction = library.CreateFunction("fragment_main");

        if (vertexFunction == null || fragmentFunction == null) return;

        var vertexDescriptor = new MTLVertexDescriptor();
        vertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float3;
        vertexDescriptor.Attributes[0].Offset = 0;
        vertexDescriptor.Attributes[0].BufferIndex = 0;
        vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float4;
        vertexDescriptor.Attributes[1].Offset = 12;
        vertexDescriptor.Attributes[1].BufferIndex = 0;
        vertexDescriptor.Layouts[0].Stride = (nuint)CubeVertex.SizeInBytes;
        vertexDescriptor.Layouts[0].StepRate = 1;
        vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

        var pipelineDescriptor = new MTLRenderPipelineDescriptor
        {
            VertexFunction = vertexFunction,
            FragmentFunction = fragmentFunction,
            VertexDescriptor = vertexDescriptor,
            DepthAttachmentPixelFormat = MTLPixelFormat.Depth32Float
        };

        pipelineDescriptor.ColorAttachments[0].PixelFormat = MTLPixelFormat.BGRA8Unorm;
        pipelineDescriptor.ColorAttachments[0].BlendingEnabled = true;
        pipelineDescriptor.ColorAttachments[0].SourceRgbBlendFactor = MTLBlendFactor.SourceAlpha;
        pipelineDescriptor.ColorAttachments[0].DestinationRgbBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;
        pipelineDescriptor.ColorAttachments[0].SourceAlphaBlendFactor = MTLBlendFactor.One;
        pipelineDescriptor.ColorAttachments[0].DestinationAlphaBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;

        Foundation.NSError pipelineError;
        _pipelineState = _device.CreateRenderPipelineState(pipelineDescriptor, out pipelineError);
    }

    private void CreateDepthStencilState()
    {
        if (_device == null) return;

        // Opaque: depth test and write enabled
        var opaqueDescriptor = new MTLDepthStencilDescriptor
        {
            DepthCompareFunction = MTLCompareFunction.Less,
            DepthWriteEnabled = true
        };
        _depthStencilStateOpaque = _device.CreateDepthStencilState(opaqueDescriptor);

        // Transparent: depth test enabled but writes disabled (for proper blending)
        var transparentDescriptor = new MTLDepthStencilDescriptor
        {
            DepthCompareFunction = MTLCompareFunction.Less,
            DepthWriteEnabled = false
        };
        _depthStencilStateTransparent = _device.CreateDepthStencilState(transparentDescriptor);
    }

    public void Resize(float width, float height)
    {
        if (PlatformView != null && width > 0 && height > 0)
        {
            PlatformView.Frame = new CGRect(0, 0, width, height);
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

        // Rebuild vertex buffer with new state
        float scale = 0.4f;
        var vertices = _bufferBuilder.BuildVertices(_root, scale, _isTransparencyOn);
        _vertexCount = vertices.Length;

        if (_vertexCount > 0)
        {
            UpdateVertexBuffer(vertices);
        }
    }

    internal void Draw(MTKView view)
    {
        if (_device == null || _commandQueue == null || _pipelineState == null) return;
        if (_vertexCount == 0) return;

        view.ClearColor = new MTLClearColor(_bgR, _bgG, _bgB, _bgA);

        var drawable = view.CurrentDrawable;
        var renderPassDescriptor = view.CurrentRenderPassDescriptor;
        if (drawable == null || renderPassDescriptor == null) return;

        var commandBuffer = _commandQueue.CommandBuffer();
        if (commandBuffer == null) return;

        var renderEncoder = commandBuffer.CreateRenderCommandEncoder(renderPassDescriptor);
        if (renderEncoder == null) return;

        renderEncoder.SetRenderPipelineState(_pipelineState);
        // Use appropriate depth state: transparent mode disables depth writes for proper blending
        var depthState = _isTransparencyOn ? _depthStencilStateTransparent : _depthStencilStateOpaque;
        if (depthState != null)
            renderEncoder.SetDepthStencilState(depthState);

        if (_vertexBuffer != null)
        {
            renderEncoder.SetVertexBuffer(_vertexBuffer, 0, 0);
            renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, (nuint)_vertexCount);
        }

        renderEncoder.EndEncoding();
        commandBuffer.PresentDrawable(drawable);
        commandBuffer.Commit();
    }

    private unsafe void UpdateVertexBuffer(CubeVertex[] vertices)
    {
        if (_device == null) return;

        nuint bufferSize = (nuint)(vertices.Length * CubeVertex.SizeInBytes);

        if (_vertexBuffer == null || _vertexBuffer.Length < bufferSize)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = _device.CreateBuffer(bufferSize, MTLResourceOptions.StorageModeShared);
        }

        if (_vertexBuffer == null) return;

        fixed (CubeVertex* ptr = vertices)
        {
            Buffer.MemoryCopy(ptr, (void*)_vertexBuffer.Contents, (long)bufferSize, (long)bufferSize);
        }
    }

    protected override void DisconnectHandler(MTKView platformView)
    {
        if (platformView is ScrollableMTKView scrollableView)
        {
            scrollableView.ScrollWheelChanged -= OnScrollWheelChanged;
        }
        platformView.Paused = true;
        platformView.Delegate = null;
        _delegate = null;
        _vertexBuffer?.Dispose();
        _pipelineState?.Dispose();
        _depthStencilStateOpaque?.Dispose();
        _depthStencilStateTransparent?.Dispose();
        base.DisconnectHandler(platformView);
    }
}

/// <summary>
/// MTKViewDelegate that calls back to the handler for rendering.
/// </summary>
class MetalViewDelegate : NSObject, IMTKViewDelegate
{
    private readonly WeakReference<MetalCubeViewHandler> _handlerRef;

    public MetalViewDelegate(MetalCubeViewHandler handler)
    {
        _handlerRef = new WeakReference<MetalCubeViewHandler>(handler);
    }

    public void DrawableSizeWillChange(MTKView view, CGSize size)
    {
        // Size change handled by Resize method
    }

    public void Draw(MTKView view)
    {
        if (_handlerRef.TryGetTarget(out var handler))
        {
            handler.Draw(view);
        }
    }
}
#endif
