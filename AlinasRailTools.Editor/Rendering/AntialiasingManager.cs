using Veldrid;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Types of antialiasing available
/// </summary>
public enum AntialiasingType
{
    None,           // No antialiasing
    MSAA_2x,        // 2x Multi-Sample Anti-Aliasing
    MSAA_4x,        // 4x Multi-Sample Anti-Aliasing
    MSAA_8x,        // 8x Multi-Sample Anti-Aliasing
    FXAA,           // Fast Approximate Anti-Aliasing (post-process)
    // Future: SMAA, TAA, etc.
}

/// <summary>
/// Manages antialiasing for the application
/// Handles framebuffer creation and resolving for MSAA, or post-process AA
/// </summary>
public class AntialiasingManager : IDisposable
{
    private readonly GraphicsDevice _device;
    private AntialiasingType _currentType;

    // MSAA resources
    private Framebuffer? _msaaFramebuffer;
    private Texture? _msaaColorTexture;
    private Texture? _msaaDepthTexture;

    // Current output description (changes based on AA type)
    public OutputDescription OutputDescription { get; private set; }

    // Whether MSAA is active (needs resolve step)
    public bool RequiresResolve => _currentType != AntialiasingType.None && _currentType != AntialiasingType.FXAA;

    public AntialiasingManager(GraphicsDevice device, AntialiasingType initialType = AntialiasingType.None)
    {
        _device = device;
        _currentType = AntialiasingType.None;
        OutputDescription = device.SwapchainFramebuffer.OutputDescription;

        if (initialType != AntialiasingType.None)
        {
            SetAntialiasingType(initialType);
        }
    }

    /// <summary>
    /// Change the antialiasing type (recreates framebuffers if needed)
    /// </summary>
    public void SetAntialiasingType(AntialiasingType type)
    {
        if (_currentType == type)
            return;

        // Dispose old resources
        DisposeResources();

        _currentType = type;

        switch (type)
        {
            case AntialiasingType.None:
                // Use swapchain directly
                OutputDescription = _device.SwapchainFramebuffer.OutputDescription;
                Console.WriteLine("[AntialiasingManager] Disabled antialiasing");
                break;

            case AntialiasingType.MSAA_2x:
                CreateMSAAFramebuffer(TextureSampleCount.Count2);
                Console.WriteLine("[AntialiasingManager] Enabled 2x MSAA");
                break;

            case AntialiasingType.MSAA_4x:
                CreateMSAAFramebuffer(TextureSampleCount.Count4);
                Console.WriteLine("[AntialiasingManager] Enabled 4x MSAA");
                break;

            case AntialiasingType.MSAA_8x:
                CreateMSAAFramebuffer(TextureSampleCount.Count8);
                Console.WriteLine("[AntialiasingManager] Enabled 8x MSAA");
                break;

            case AntialiasingType.FXAA:
                // FXAA is post-process, use swapchain but mark for post-processing
                OutputDescription = _device.SwapchainFramebuffer.OutputDescription;
                Console.WriteLine("[AntialiasingManager] Enabled FXAA (post-process)");
                // TODO: Implement FXAA shader pass
                break;
        }
    }

    /// <summary>
    /// Get the framebuffer to render to (either MSAA or swapchain)
    /// </summary>
    public Framebuffer GetRenderFramebuffer()
    {
        return _msaaFramebuffer ?? _device.SwapchainFramebuffer;
    }

    /// <summary>
    /// Resolve MSAA framebuffer to swapchain (call after rendering)
    /// </summary>
    public void ResolveToSwapchain(CommandList cl)
    {
        if (_msaaFramebuffer == null || _msaaColorTexture == null)
            return;

        // Resolve MSAA texture to swapchain
        cl.ResolveTexture(
            _msaaColorTexture,
            _device.SwapchainFramebuffer.ColorTargets[0].Target);
    }

    /// <summary>
    /// Create MSAA framebuffer with specified sample count
    /// </summary>
    private void CreateMSAAFramebuffer(TextureSampleCount sampleCount)
    {
        var swapchain = _device.SwapchainFramebuffer;
        uint width = swapchain.Width;
        uint height = swapchain.Height;

        // Create MSAA color texture
        _msaaColorTexture = _device.ResourceFactory.CreateTexture(new TextureDescription(
            width, height,
            depth: 1,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.B8_G8_R8_A8_UNorm,
            TextureUsage.RenderTarget | TextureUsage.Sampled,
            TextureType.Texture2D,
            sampleCount));

        // Create MSAA depth texture
        _msaaDepthTexture = _device.ResourceFactory.CreateTexture(new TextureDescription(
            width, height,
            depth: 1,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.R32_Float,  // Match swapchain depth format
            TextureUsage.DepthStencil,
            TextureType.Texture2D,
            sampleCount));

        // Create MSAA framebuffer
        _msaaFramebuffer = _device.ResourceFactory.CreateFramebuffer(new FramebufferDescription(
            _msaaDepthTexture,
            _msaaColorTexture));

        // Update output description for pipeline creation
        OutputDescription = _msaaFramebuffer.OutputDescription;
    }

    /// <summary>
    /// Handle window resize (recreate framebuffers)
    /// </summary>
    public void HandleResize()
    {
        if (_currentType == AntialiasingType.None || _currentType == AntialiasingType.FXAA)
            return;

        // Recreate MSAA framebuffer with new size
        var type = _currentType;
        DisposeResources();
        _currentType = AntialiasingType.None; // Force recreation
        SetAntialiasingType(type);
    }

    private void DisposeResources()
    {
        _msaaFramebuffer?.Dispose();
        _msaaColorTexture?.Dispose();
        _msaaDepthTexture?.Dispose();

        _msaaFramebuffer = null;
        _msaaColorTexture = null;
        _msaaDepthTexture = null;
    }

    public void Dispose()
    {
        DisposeResources();
        Console.WriteLine("[AntialiasingManager] Disposed");
    }
}
