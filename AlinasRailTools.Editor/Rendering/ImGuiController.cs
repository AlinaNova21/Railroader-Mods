using System.Numerics;
using ImGuiNET;
using Veldrid;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Simple ImGui controller for Veldrid
/// </summary>
public class ImGuiController : IDisposable
{
    private readonly ImGuiRenderer _renderer;

    public ImGuiController(GraphicsDevice device, OutputDescription outputDescription, int width, int height)
    {
        _renderer = new ImGuiRenderer(device, outputDescription, width, height);
    }

    public void Update(float deltaTime, InputSnapshot snapshot)
    {
        _renderer.Update(deltaTime, snapshot);
    }

    public void Render(GraphicsDevice device, CommandList commandList)
    {
        _renderer.Render(device, commandList);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
