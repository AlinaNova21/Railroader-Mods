using System.Numerics;
using ImGuiNET;
using Serilog.Events;
using AlinasRailTools.Editor.Logging;

namespace AlinasRailTools.Editor.UI;

/// <summary>
/// In-game debug console window that displays logs and accepts commands
/// Toggles with the ` (grave/tilde) key
/// </summary>
public class DebugConsoleWindow
{
    private readonly InGameConsoleSink _consoleSink;
    private bool _isVisible = false;
    private string _commandInput = string.Empty;
    private bool _scrollToBottom = false;
    private bool _autoScroll = true;

    public DebugConsoleWindow(InGameConsoleSink consoleSink)
    {
        _consoleSink = consoleSink;
    }

    /// <summary>
    /// Toggle console visibility
    /// </summary>
    public void Toggle()
    {
        _isVisible = !_isVisible;
    }

    /// <summary>
    /// Show the console
    /// </summary>
    public void Show() => _isVisible = true;

    /// <summary>
    /// Hide the console
    /// </summary>
    public void Hide() => _isVisible = false;

    /// <summary>
    /// Check if console is visible
    /// </summary>
    public bool IsVisible => _isVisible;

    /// <summary>
    /// Render the console window
    /// </summary>
    public void Render()
    {
        if (!_isVisible)
            return;

        // Set window to take up top half of screen
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, viewport.Size.Y * 0.5f));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoResize |
                                  ImGuiWindowFlags.NoMove |
                                  ImGuiWindowFlags.NoCollapse |
                                  ImGuiWindowFlags.NoTitleBar;

        if (ImGui.Begin("DebugConsole", ref _isVisible, flags))
        {

            // Log display area
            ImGuiWindowFlags childFlags = ImGuiWindowFlags.HorizontalScrollbar;
            var availableSpace = ImGui.GetContentRegionAvail();
            ImGui.BeginChild("ScrollingRegion", new Vector2(0, availableSpace.Y - 30), true, childFlags);

            // Display logs
            var logs = _consoleSink.GetAllLogs();
            foreach (var log in logs)
            {
                // Color-code by log level
                Vector4 color = log.Level switch
                {
                    LogEventLevel.Verbose => new Vector4(0.7f, 0.7f, 0.7f, 1.0f),   // Gray
                    LogEventLevel.Debug => new Vector4(0.5f, 0.5f, 0.5f, 1.0f),     // Dark Gray
                    LogEventLevel.Information => new Vector4(0.9f, 0.9f, 0.9f, 1.0f), // White
                    LogEventLevel.Warning => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),   // Yellow
                    LogEventLevel.Error => new Vector4(1.0f, 0.4f, 0.4f, 1.0f),     // Red
                    LogEventLevel.Fatal => new Vector4(1.0f, 0.0f, 0.0f, 1.0f),     // Bright Red
                    _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                };

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextUnformatted(log.Message);
                ImGui.PopStyleColor();
            }

            // Auto-scroll to bottom
            if (_scrollToBottom || (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY()))
            {
                ImGui.SetScrollHereY(1.0f);
            }
            _scrollToBottom = false;

            ImGui.EndChild();

            // Command input and controls at bottom
            ImGui.Separator();

            // Input field takes most of the width
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 190); // Leave room for buttons
            if (ImGui.InputText("##CommandInput", ref _commandInput, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (!string.IsNullOrWhiteSpace(_commandInput))
                {
                    ExecuteCommand(_commandInput);
                    _commandInput = string.Empty;
                    _scrollToBottom = true;
                }
                ImGui.SetKeyboardFocusHere(-1); // Keep focus on input
            }

            // Set focus to input when console opens
            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetKeyboardFocusHere(-1);
            }

            // Controls inline with input
            ImGui.SameLine();
            ImGui.Checkbox("Auto", ref _autoScroll);

            ImGui.SameLine();
            if (ImGui.Button("Clear"))
            {
                _consoleSink.Clear();
            }
        }
        ImGui.End();
    }

    /// <summary>
    /// Execute a console command (stubbed for now)
    /// </summary>
    private void ExecuteCommand(string command)
    {
        Serilog.Log.Information("[Console] Command: {Command}", command);

        // TODO: Implement command system
        // For now, just echo the command
        if (command.StartsWith("help", StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Information("[Console] Available commands: help, clear, quit");
        }
        else if (command.StartsWith("clear", StringComparison.OrdinalIgnoreCase))
        {
            _consoleSink.Clear();
        }
        else if (command.StartsWith("quit", StringComparison.OrdinalIgnoreCase) ||
                 command.StartsWith("exit", StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Information("[Console] Use Alt+F4 or close window to quit");
        }
        else
        {
            Serilog.Log.Warning("[Console] Unknown command: {Command}. Type 'help' for available commands.", command);
        }
    }
}
