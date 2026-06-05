using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that manages Level of Detail by toggling MeshRendererComponents based on distance
/// Queries entities with LODComponent and enables/disables appropriate LOD level renderers
/// </summary>
public class LODSystem : ILODSystem
{
    private readonly ILogger _logger;

    public LODSystem()
    {
        _logger = Log.ForContext<LODSystem>();
        _logger.Information("Initialized");
    }

    /// <summary>
    /// Update LOD levels for all entities based on camera distance
    /// </summary>
    public void Update(World world, Camera camera)
    {
        // Camera.Position is in Veldrid space (-Z forward), convert to Unity space for comparison
        Vector3 cameraPosVeldrid = camera.Position;
        Vector3 cameraPosUnity = new Vector3(cameraPosVeldrid.X, cameraPosVeldrid.Y, -cameraPosVeldrid.Z);

        // Query all entities with LOD component and Transform
        var query = new QueryDescription()
            .WithAll<LODComponent, Transform>();

        world.Query(in query, (Entity entity, ref LODComponent lodComp, ref Transform transform) =>
        {
            // Calculate distance from camera to entity (both in Unity space now)
            float distance = Vector3.Distance(cameraPosUnity, transform.Position);

            // Determine which LOD level should be active
            int targetLevel = lodComp.GetLevelForDistance(distance);

            // Only update if level changed
            if (targetLevel != lodComp.CurrentLevel)
            {
                // Update LOD component
                lodComp.CurrentLevel = targetLevel;
                world.Set(entity, lodComp);

                // Toggle all MeshRendererComponents on this entity based on LOD level
                UpdateRenderers(world, entity, targetLevel);
            }
        });
    }

    /// <summary>
    /// Enable/disable renderers based on target LOD level
    /// Toggles MeshRendererComponent.Enabled on child entities
    /// </summary>
    private void UpdateRenderers(World world, Entity entity, int targetLevel)
    {
        var lodComp = entity.Get<LODComponent>();

        // If we have child entities, toggle their renderer components
        if (lodComp.ChildEntities != null && lodComp.ChildEntities.Length > 0)
        {
            for (int i = 0; i < lodComp.ChildEntities.Length; i++)
            {
                var childEntity = lodComp.ChildEntities[i];
                if (!world.IsAlive(childEntity))
                    continue;

                bool shouldBeActive = (i == targetLevel);

                // Toggle MeshRendererComponent.Enabled
                if (childEntity.Has<MeshRendererComponent>())
                {
                    var renderer = childEntity.Get<MeshRendererComponent>();
                    if (renderer.Enabled != shouldBeActive)
                    {
                        renderer.Enabled = shouldBeActive;
                        world.Set(childEntity, renderer);
                    }
                }

                // Toggle MeshRendererListComponent.Enabled (for ties)
                if (childEntity.Has<MeshRendererListComponent>())
                {
                    var listRenderer = childEntity.Get<MeshRendererListComponent>();
                    if (listRenderer.Enabled != shouldBeActive)
                    {
                        listRenderer.Enabled = shouldBeActive;
                        world.Set(childEntity, listRenderer);
                    }
                }
            }
        }
        // Fallback to old behavior for MeshRendererCollectionComponent
        else if (entity.Has<MeshRendererCollectionComponent>())
        {
            var collection = entity.Get<MeshRendererCollectionComponent>();
            collection.SetActiveLevel(targetLevel);
            world.Set(entity, collection);
        }
        // Fallback to old behavior for single MeshRendererComponent
        else if (entity.Has<MeshRendererComponent>())
        {
            var renderer = entity.Get<MeshRendererComponent>();

            // Enable if renderer's LOD level matches target
            bool shouldEnable = (renderer.LODLevel == targetLevel);

            if (renderer.Enabled != shouldEnable)
            {
                renderer.Enabled = shouldEnable;
                world.Set(entity, renderer);
            }
        }
    }

    public void Dispose()
    {
        _logger.Information("Disposed");
    }
}
