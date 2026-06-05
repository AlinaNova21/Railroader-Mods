using Veldrid;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Serilog;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Veldrid texture wrapper
/// </summary>
public class VeldridTexture : IDisposable
{
    private static readonly ILogger _logger = Log.ForContext<VeldridTexture>();
    private readonly GraphicsDevice _device;
    private readonly Texture _texture;
    private readonly TextureView _textureView;

    public Texture Texture => _texture;
    public TextureView TextureView => _textureView;
    public uint Width => _texture.Width;
    public uint Height => _texture.Height;

    private VeldridTexture(GraphicsDevice device, Texture texture)
    {
        _device = device;
        _texture = texture;
        _textureView = device.ResourceFactory.CreateTextureView(texture);
    }

    /// <summary>
    /// Load texture from file using ImageSharp
    /// </summary>
    public static VeldridTexture FromFile(GraphicsDevice device, string path)
    {
        ImageSharpTexture imageSharpTex = new ImageSharpTexture(path);
        Texture texture = imageSharpTex.CreateDeviceTexture(device, device.ResourceFactory);
        _logger.Information("Loaded texture from {Path}: {Width}x{Height}", path, texture.Width, texture.Height);
        return new VeldridTexture(device, texture);
    }

    /// <summary>
    /// Create R16 heightmap texture from byte array
    /// ImageSharp will handle the data upload
    /// </summary>
    public static VeldridTexture CreateR16FromBytes(GraphicsDevice device, int width, int height, byte[] data)
    {
        _logger.Debug("Creating R16 texture {Width}x{Height}, {DataLength} bytes", width, height, data.Length);

        // Validate data size
        int expectedSize = width * height * 2; // 2 bytes per pixel (R16)
        if (data.Length != expectedSize)
        {
            throw new ArgumentException($"Data size mismatch: expected {expectedSize} bytes, got {data.Length}");
        }

        // Create Veldrid texture
        Texture texture = device.ResourceFactory.CreateTexture(new TextureDescription(
            (uint)width,
            (uint)height,
            1, // depth
            1, // mipLevels
            1, // arrayLayers
            PixelFormat.R16_UNorm, // 16-bit normalized red channel
            TextureUsage.Sampled,
            TextureType.Texture2D
        ));

        // Upload data to GPU
        device.UpdateTexture(
            texture,
            data,
            0, 0, 0, // x, y, z
            (uint)width, (uint)height, 1, // width, height, depth
            0, // mipLevel
            0  // arrayLayer
        );

        _logger.Debug("R16 texture created successfully");
        return new VeldridTexture(device, texture);
    }

    /// <summary>
    /// Create R32_Float heightmap texture from normalized float array (0-1 range)
    /// More memory than R16 but eliminates packing/unpacking overhead
    /// </summary>
    public static VeldridTexture CreateR32FromFloats(GraphicsDevice device, int width, int height, float[] data)
    {
        _logger.Debug("Creating R32_Float texture {Width}x{Height}, {DataLength} floats", width, height, data.Length);

        // Validate data size
        int expectedSize = width * height;
        if (data.Length != expectedSize)
        {
            throw new ArgumentException($"Data size mismatch: expected {expectedSize} floats, got {data.Length}");
        }

        // Create Veldrid texture
        Texture texture = device.ResourceFactory.CreateTexture(new TextureDescription(
            (uint)width,
            (uint)height,
            1, // depth
            1, // mipLevels
            1, // arrayLayers
            PixelFormat.R32_Float, // 32-bit float red channel
            TextureUsage.Sampled,
            TextureType.Texture2D
        ));

        // Upload data to GPU (float array)
        device.UpdateTexture(
            texture,
            data,
            0, 0, 0, // x, y, z
            (uint)width, (uint)height, 1, // width, height, depth
            0, // mipLevel
            0  // arrayLayer
        );

        _logger.Debug("R32_Float texture created successfully");
        return new VeldridTexture(device, texture);
    }

    /// <summary>
    /// Create RGBA texture from byte array
    /// </summary>
    public static VeldridTexture CreateRGBA(GraphicsDevice device, int width, int height, byte[] data)
    {
        _logger.Debug("Creating RGBA texture {Width}x{Height}", width, height);

        Texture texture = device.ResourceFactory.CreateTexture(new TextureDescription(
            (uint)width,
            (uint)height,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D
        ));

        device.UpdateTexture(texture, data, 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

        return new VeldridTexture(device, texture);
    }

    /// <summary>
    /// Create texture from ImageSharp image
    /// </summary>
    public static VeldridTexture FromImage(GraphicsDevice device, Image<Rgba32> image)
    {
        ImageSharpTexture imageSharpTex = new ImageSharpTexture(image);
        Texture texture = imageSharpTex.CreateDeviceTexture(device, device.ResourceFactory);
        return new VeldridTexture(device, texture);
    }

    public void Dispose()
    {
        _textureView.Dispose();
        _texture.Dispose();
    }
}
