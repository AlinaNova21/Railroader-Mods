namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Tag component that marks an entity as renderable.
/// Entities with this component will be processed by render systems that require it.
/// For LOD-controlled entities, use MeshRendererComponent.Enabled instead.
/// </summary>
public struct RenderableComponent
{
    // Tag component - no data needed
}
