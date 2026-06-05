using System.Numerics;
using ImGuiNET;
using AlinasRailTools.Editor.ECS;

namespace AlinasRailTools.Editor.UI;

/// <summary>
/// Debug window that shows per-system performance timing data
/// Useful for identifying bottlenecks and optimizing frame time
/// </summary>
public class PerformanceStatsWindow
{
    private readonly SystemManager _systemManager;
#if DEBUG
    private bool _isVisible = true; // Visible by default in debug builds
#else
    private bool _isVisible = false; // Hidden by default in release builds
#endif
    private bool _sortByTime = true; // true = sort by time (slowest first), false = sort by name

    public PerformanceStatsWindow(SystemManager systemManager)
    {
        _systemManager = systemManager;
    }

    /// <summary>
    /// Toggle window visibility
    /// </summary>
    public void Toggle() => _isVisible = !_isVisible;

    /// <summary>
    /// Render the performance stats window
    /// </summary>
    public void Render()
    {
        if (!_isVisible)
            return;

        ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Performance Stats", ref _isVisible))
        {
            var timings = _systemManager.GetSystemTimings();

            if (timings.Count == 0)
            {
                ImGui.TextDisabled("No performance data yet...");
            }
            else
            {
                // Calculate totals
                double totalUpdateTime = 0.0;
                double totalRenderTime = 0.0;
                double totalTime = 0.0;

                foreach (var kvp in timings)
                {
                    totalTime += kvp.Value;
                    if (kvp.Key.StartsWith("Update."))
                        totalUpdateTime += kvp.Value;
                    else if (kvp.Key.StartsWith("Render."))
                        totalRenderTime += kvp.Value;
                }

                // Display totals
                ImGui.Text($"Total Update: {totalUpdateTime:F2}ms");
                ImGui.SameLine();
                ImGui.Text($"Total Render: {totalRenderTime:F2}ms");
                ImGui.SameLine();
                ImGui.Text($"Total: {totalTime:F2}ms");

                ImGui.Separator();

                // Sort option
                if (ImGui.Checkbox("Sort by Time", ref _sortByTime))
                {
                    // Checkbox state changed
                }

                ImGui.Separator();

                // Create table for performance data
                if (ImGui.BeginTable("PerformanceTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Phase", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("System", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Time (ms)", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableHeadersRow();

                    // Sort timings
                    var sortedTimings = _sortByTime
                        ? timings.OrderByDescending(x => x.Value)
                        : timings.OrderBy(x => x.Key);

                    foreach (var kvp in sortedTimings)
                    {
                        string fullName = kvp.Key;
                        double timeMs = kvp.Value;

                        // Split into phase and system name
                        string phase = "";
                        string systemName = fullName;
                        int dotIndex = fullName.IndexOf('.');
                        if (dotIndex > 0)
                        {
                            phase = fullName.Substring(0, dotIndex);
                            systemName = fullName.Substring(dotIndex + 1);
                        }

                        ImGui.TableNextRow();

                        // Phase column (color-coded)
                        ImGui.TableNextColumn();
                        Vector4 phaseColor = phase switch
                        {
                            "Update" => new Vector4(0.4f, 0.8f, 1.0f, 1.0f), // Light blue
                            "Render" => new Vector4(1.0f, 0.8f, 0.4f, 1.0f), // Orange
                            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                        };
                        ImGui.TextColored(phaseColor, phase);

                        // System name column
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(systemName);

                        // Time column (color-coded by performance)
                        ImGui.TableNextColumn();
                        Vector4 timeColor = timeMs switch
                        {
                            >= 10.0 => new Vector4(1.0f, 0.2f, 0.2f, 1.0f),  // Red (>10ms is bad)
                            >= 5.0 => new Vector4(1.0f, 0.8f, 0.2f, 1.0f),   // Yellow (5-10ms is concerning)
                            >= 1.0 => new Vector4(1.0f, 1.0f, 0.4f, 1.0f),   // Light yellow (1-5ms is okay)
                            _ => new Vector4(0.4f, 1.0f, 0.4f, 1.0f)          // Green (<1ms is great)
                        };
                        ImGui.TextColored(timeColor, $"{timeMs:F2}");
                    }

                    ImGui.EndTable();
                }
            }
        }
        ImGui.End();
    }
}
