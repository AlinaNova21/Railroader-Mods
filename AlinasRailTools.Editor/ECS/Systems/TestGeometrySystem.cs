using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Veldrid;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that creates test geometry for debugging rendering
/// Creates a 4x4x4 grid of cubes and a single rectangle for testing
/// </summary>
public class TestGeometrySystem : ISystem
{
    private readonly World _world;
    private readonly MeshManager _meshManager;
    private readonly GraphicsDevice _device;
    private Guid _cubeMeshId;
    private Guid _rectangleMeshId;
    private readonly List<Entity> _testEntities = new();

    public TestGeometrySystem(World world, MeshManager meshManager, GraphicsDevice device)
    {
        _world = world;
        _meshManager = meshManager;
        _device = device;

        CreateTestGeometry();
        Console.WriteLine("[TestGeometrySystem] Initialized");
    }

    private void CreateTestGeometry()
    {
        // Create cube mesh (1x1x1 unit cube)
        var cubeMesh = CreateCubeMesh();
        _cubeMeshId = _meshManager.RegisterMesh(cubeMesh);

        // Create rectangle mesh (2x2x0.1)
        var rectangleMesh = CreateRectangleMesh();
        _rectangleMeshId = _meshManager.RegisterMesh(rectangleMesh);

        // Material for cubes (cyan, supports instancing)
        var cubeMaterial = Material.CreateColored(new Vector4(0, 1, 1, 1), supportsInstancing: true);
        // Disable back-face culling for debugging
        cubeMaterial.RasterizerState = new RasterizerStateDescription(
            cullMode: FaceCullMode.None,
            fillMode: PolygonFillMode.Solid,
            frontFace: FrontFace.CounterClockwise,
            depthClipEnabled: true,
            scissorTestEnabled: false);

        // Material for rectangle (magenta, no instancing)
        var rectangleMaterial = Material.CreateColored(new Vector4(1, 0, 1, 1), supportsInstancing: false);

        // Create ONE GIANT cube at origin for debugging
        var giantCube = _world.Create(
            new Transform { Position = Vector3.Zero, Scale = new Vector3(500, 500, 500) }, // HUGE cube at origin
            new MeshRendererComponent(_cubeMeshId, cubeMaterial),
            new RenderableComponent(),
            new BoundingBoxComponent(new BoundingBox(
                new Vector3(-250, -250, -250),
                new Vector3(250, 250, 250)))
        );
        _meshManager.AddReference(_cubeMeshId);
        _testEntities.Add(giantCube);

        // Create a few cubes right next to the giant cube for easy visibility
        for (int i = 0; i < 3; i++)
        {
            var testCube = _world.Create(
                new Transform { Position = new Vector3(600 + i * 150, 0, 0), Scale = new Vector3(100, 100, 100) },
                new MeshRendererComponent(_cubeMeshId, cubeMaterial),
                new RenderableComponent(),
                new BoundingBoxComponent(new BoundingBox(
                    new Vector3(-50, -50, -50),
                    new Vector3(50, 50, 50)))
            );
            _meshManager.AddReference(_cubeMeshId);
            _testEntities.Add(testCube);
        }

        // Create 4x4x4 grid of cubes spread out more for visibility
        // Camera is at (750, 800, 250) looking at (750, 500, 750)
        Vector3 gridOrigin = new Vector3(1000, 500, 0); // Far from giant cube, easy to see
        float spacing = 200.0f; // 200 units between cube centers (much more spread out)

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    Vector3 position = gridOrigin + new Vector3(x * spacing, y * spacing, z * spacing);

                    var entity = _world.Create(
                        new Transform { Position = position, Scale = new Vector3(100, 100, 100) }, // Much bigger cubes (100x100x100)
                        new MeshRendererComponent(_cubeMeshId, cubeMaterial),
                        new RenderableComponent(),
                        new BoundingBoxComponent(new BoundingBox(
                            new Vector3(-50, -50, -50),
                            new Vector3(50, 50, 50)))
                    );

                    _meshManager.AddReference(_cubeMeshId);
                    _testEntities.Add(entity);
                }
            }
        }

        // Create single rectangle above the cubes
        Vector3 rectanglePosition = gridOrigin + new Vector3(30, 120, 30); // Centered above grid, 120 units up

        var rectangleEntity = _world.Create(
            new Transform { Position = rectanglePosition, Scale = new Vector3(20, 20, 20) }, // Make rectangle 20x bigger
            new MeshRendererComponent(_rectangleMeshId, rectangleMaterial),
            new RenderableComponent(),
            new BoundingBoxComponent(new BoundingBox(
                new Vector3(-20, -20, -1),
                new Vector3(20, 20, 1)))
        );

        _meshManager.AddReference(_rectangleMeshId);
        _testEntities.Add(rectangleEntity);

        Console.WriteLine($"[TestGeometrySystem] Created {_testEntities.Count} test entities (1 giant cube + 3 nearby cubes + 64 grid cubes + 1 rectangle)");
    }

    private VeldridMesh CreateCubeMesh()
    {
        // Create a 1x1x1 cube centered at origin
        var vertices = new VeldridMesh.ColorVertex[]
        {
            // Front face (cyan)
            new VeldridMesh.ColorVertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, 1, 1)),
            new VeldridMesh.ColorVertex(new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, 1, 1)),
            new VeldridMesh.ColorVertex(new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0, 1, 1)),
            new VeldridMesh.ColorVertex(new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0, 1, 1)),

            // Back face (darker cyan)
            new VeldridMesh.ColorVertex(new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0, 0.7f, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0, 0.7f, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0, 0.7f, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0, 0.7f, 0.7f)),
        };

        var indices = new uint[]
        {
            // Front face
            0, 1, 2, 2, 3, 0,
            // Back face
            5, 4, 7, 7, 6, 5,
            // Left face
            4, 0, 3, 3, 7, 4,
            // Right face
            1, 5, 6, 6, 2, 1,
            // Top face
            3, 2, 6, 6, 7, 3,
            // Bottom face
            4, 5, 1, 1, 0, 4
        };

        return VeldridMesh.CreateColored(_device, vertices, indices);
    }

    private VeldridMesh CreateRectangleMesh()
    {
        // Create a 2x2x0.1 rectangle (flat, oriented horizontally)
        var vertices = new VeldridMesh.ColorVertex[]
        {
            // Top face (magenta)
            new VeldridMesh.ColorVertex(new Vector3(-1, -1, -0.05f), new Vector3(1, 0, 1)),
            new VeldridMesh.ColorVertex(new Vector3(1, -1, -0.05f), new Vector3(1, 0, 1)),
            new VeldridMesh.ColorVertex(new Vector3(1, 1, -0.05f), new Vector3(1, 0, 1)),
            new VeldridMesh.ColorVertex(new Vector3(-1, 1, -0.05f), new Vector3(1, 0, 1)),

            // Bottom face (darker magenta)
            new VeldridMesh.ColorVertex(new Vector3(-1, -1, 0.05f), new Vector3(0.7f, 0, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(1, -1, 0.05f), new Vector3(0.7f, 0, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(1, 1, 0.05f), new Vector3(0.7f, 0, 0.7f)),
            new VeldridMesh.ColorVertex(new Vector3(-1, 1, 0.05f), new Vector3(0.7f, 0, 0.7f)),
        };

        var indices = new uint[]
        {
            // Top face
            0, 1, 2, 2, 3, 0,
            // Bottom face
            5, 4, 7, 7, 6, 5,
            // Sides
            4, 0, 3, 3, 7, 4,
            1, 5, 6, 6, 2, 1,
            3, 2, 6, 6, 7, 3,
            4, 5, 1, 1, 0, 4
        };

        return VeldridMesh.CreateColored(_device, vertices, indices);
    }

    public void Dispose()
    {
        // Remove references for all test entities
        foreach (var entity in _testEntities)
        {
            if (entity.Has<MeshRendererComponent>())
            {
                var renderer = entity.Get<MeshRendererComponent>();
                _meshManager.RemoveReference(renderer.MeshId);
            }
        }

        Console.WriteLine("[TestGeometrySystem] Disposed");
    }
}
