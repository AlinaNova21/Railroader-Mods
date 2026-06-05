namespace AlinasRailTools.Editor.ECS;

/// <summary>
/// Base interface for all ECS systems
/// Enforces IDisposable pattern for proper resource cleanup
/// </summary>
public interface ISystem : IDisposable
{
}
