using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Veldrid mesh with vertex and index buffers
/// Each mesh has a unique ID for tracking and reference counting
/// </summary>
public class VeldridMesh : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer? _indexBuffer;
    private readonly uint _vertexCount;
    private readonly uint _indexCount;
    private readonly bool _hasIndices;
    private int _referenceCount;

    /// <summary>
    /// Unique identifier for this mesh instance
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Number of components/entities referencing this mesh
    /// </summary>
    public int ReferenceCount => _referenceCount;

    public DeviceBuffer VertexBuffer => _vertexBuffer;
    public DeviceBuffer? IndexBuffer => _indexBuffer;
    public uint VertexCount => _vertexCount;
    public uint IndexCount => _indexCount;
    public bool HasIndices => _hasIndices;

    /// <summary>
    /// Vertex format for terrain: Position + TexCoord + Normal
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector3 Normal;

        public TerrainVertex(Vector3 position, Vector2 texCoord, Vector3 normal)
        {
            Position = position;
            TexCoord = texCoord;
            Normal = normal;
        }

        public static uint SizeInBytes => (uint)Marshal.SizeOf<TerrainVertex>();

        public static void PrintLayout()
        {
            Console.WriteLine($"[TerrainVertex] Total size: {Marshal.SizeOf<TerrainVertex>()} bytes");
            Console.WriteLine($"[TerrainVertex] Position offset: {Marshal.OffsetOf<TerrainVertex>(nameof(Position))}");
            Console.WriteLine($"[TerrainVertex] TexCoord offset: {Marshal.OffsetOf<TerrainVertex>(nameof(TexCoord))}");
            Console.WriteLine($"[TerrainVertex] Normal offset: {Marshal.OffsetOf<TerrainVertex>(nameof(Normal))}");
        }
    }

    /// <summary>
    /// Simple vertex format: Position + Color
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ColorVertex
    {
        public Vector3 Position;
        public Vector3 Color;

        public ColorVertex(Vector3 position, Vector3 color)
        {
            Position = position;
            Color = color;
        }

        public static uint SizeInBytes => (uint)Marshal.SizeOf<ColorVertex>();
    }

    private VeldridMesh(GraphicsDevice device, DeviceBuffer vertexBuffer, DeviceBuffer? indexBuffer, uint vertexCount, uint indexCount)
    {
        _device = device;
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _vertexCount = vertexCount;
        _indexCount = indexCount;
        _hasIndices = indexBuffer != null;
        _referenceCount = 0;
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Increment reference count (called when a component starts using this mesh)
    /// </summary>
    public void AddReference()
    {
        _referenceCount++;
    }

    /// <summary>
    /// Decrement reference count (called when a component stops using this mesh)
    /// Returns true if reference count reached zero
    /// </summary>
    public bool RemoveReference()
    {
        if (_referenceCount > 0)
        {
            _referenceCount--;
        }
        return _referenceCount == 0;
    }

    /// <summary>
    /// Create mesh from terrain vertices (Position + TexCoord + Normal)
    /// </summary>
    public static VeldridMesh CreateTerrain(GraphicsDevice device, TerrainVertex[] vertices, uint[]? indices = null)
    {
        // Create vertex buffer
        BufferDescription vbDesc = new BufferDescription(
            (uint)(vertices.Length * Marshal.SizeOf<TerrainVertex>()),
            BufferUsage.VertexBuffer
        );
        DeviceBuffer vertexBuffer = device.ResourceFactory.CreateBuffer(vbDesc);
        device.UpdateBuffer(vertexBuffer, 0, vertices);

        // Create index buffer if indices provided
        DeviceBuffer? indexBuffer = null;
        uint indexCount = 0;
        if (indices != null)
        {
            BufferDescription ibDesc = new BufferDescription(
                (uint)(indices.Length * sizeof(uint)),
                BufferUsage.IndexBuffer
            );
            indexBuffer = device.ResourceFactory.CreateBuffer(ibDesc);
            device.UpdateBuffer(indexBuffer, 0, indices);
            indexCount = (uint)indices.Length;
        }

        Console.WriteLine($"[VeldridMesh] Created terrain mesh: {vertices.Length} vertices, {(indices != null ? $"{indices.Length} indices" : "no indices")}");

        return new VeldridMesh(device, vertexBuffer, indexBuffer, (uint)vertices.Length, indexCount);
    }

    /// <summary>
    /// Create mesh from colored vertices (Position + Color)
    /// </summary>
    public static VeldridMesh CreateColored(GraphicsDevice device, ColorVertex[] vertices, uint[]? indices = null)
    {
        // Create vertex buffer
        BufferDescription vbDesc = new BufferDescription(
            (uint)(vertices.Length * Marshal.SizeOf<ColorVertex>()),
            BufferUsage.VertexBuffer
        );
        DeviceBuffer vertexBuffer = device.ResourceFactory.CreateBuffer(vbDesc);
        device.UpdateBuffer(vertexBuffer, 0, vertices);

        // Create index buffer if indices provided
        DeviceBuffer? indexBuffer = null;
        uint indexCount = 0;
        if (indices != null)
        {
            BufferDescription ibDesc = new BufferDescription(
                (uint)(indices.Length * sizeof(uint)),
                BufferUsage.IndexBuffer
            );
            indexBuffer = device.ResourceFactory.CreateBuffer(ibDesc);
            device.UpdateBuffer(indexBuffer, 0, indices);
            indexCount = (uint)indices.Length;
        }

        Console.WriteLine($"[VeldridMesh] Created colored mesh: {vertices.Length} vertices, {(indices != null ? $"{indices.Length} indices" : "no indices")}");

        return new VeldridMesh(device, vertexBuffer, indexBuffer, (uint)vertices.Length, indexCount);
    }

    /// <summary>
    /// Generate a plane mesh (for terrain tiles)
    /// </summary>
    public static VeldridMesh CreatePlane(GraphicsDevice device, float width, float height, int subdivisionsX, int subdivisionsZ)
    {
        List<TerrainVertex> vertices = new();
        List<uint> indices = new();

        float halfWidth = width / 2.0f;
        float halfHeight = height / 2.0f;

        // Generate vertices
        for (int z = 0; z <= subdivisionsZ; z++)
        {
            for (int x = 0; x <= subdivisionsX; x++)
            {
                float xPos = ((float)x / subdivisionsX) * width - halfWidth;
                float zPos = ((float)z / subdivisionsZ) * height - halfHeight;
                float u = (float)x / subdivisionsX;
                float v = (float)z / subdivisionsZ;

                vertices.Add(new TerrainVertex(
                    new Vector3(xPos, 0.0f, zPos),
                    new Vector2(u, v),
                    new Vector3(0.0f, 1.0f, 0.0f) // Normal pointing up
                ));
            }
        }

        // Generate indices
        for (int z = 0; z < subdivisionsZ; z++)
        {
            for (int x = 0; x < subdivisionsX; x++)
            {
                uint topLeft = (uint)(z * (subdivisionsX + 1) + x);
                uint topRight = topLeft + 1;
                uint bottomLeft = (uint)((z + 1) * (subdivisionsX + 1) + x);
                uint bottomRight = bottomLeft + 1;

                // First triangle
                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(topRight);

                // Second triangle
                indices.Add(topRight);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
            }
        }

        return CreateTerrain(device, vertices.ToArray(), indices.ToArray());
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer?.Dispose();
    }
}
