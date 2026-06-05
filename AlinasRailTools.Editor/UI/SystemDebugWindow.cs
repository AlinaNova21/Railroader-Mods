using System.Numerics;
using ImGuiNET;
using Serilog.Events;
using AlinasRailTools.Editor.Logging;

namespace AlinasRailTools.Editor.UI;

/// <summary>
/// Debug window that shows the last log message from each system
/// Useful for quick status overview
/// </summary>
public class SystemDebugWindow
{
    private readonly InGameConsoleSink _consoleSink;
    private bool _isVisible = true;

    public SystemDebugWindow(InGameConsoleSink consoleSink)
    {
        _consoleSink = consoleSink;
    }

    /// <summary>
    /// Toggle window visibility
    /// </summary>
    public void Toggle() => _isVisible = !_isVisible;

    /// <summary>
    /// Render the system debug window
    /// </summary>
    public void Render()
    {
        if (!_isVisible)
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("System Status", ref _isVisible))
        {
            var lastLogs = _consoleSink.GetLastLogPerSystem();

            if (lastLogs.Count == 0)
            {
                ImGui.TextDisabled("No system logs yet...");
            }
            else
            {
                // Create table for better organization
                if (ImGui.BeginTable("SystemLogsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("System", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 70);
                    ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var kvp in lastLogs.OrderBy(x => x.Key))
                    {
                        var systemName = kvp.Key;
                        var log = kvp.Value;

                        ImGui.TableNextRow();

                        // System name column
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(GetShortSystemName(systemName));

                        // Level column (color-coded)
                        ImGui.TableNextColumn();
                        Vector4 levelColor = log.Level switch
                        {
                            LogEventLevel.Verbose => new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                            LogEventLevel.Debug => new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                            LogEventLevel.Information => new Vector4(0.4f, 0.8f, 0.4f, 1.0f), // Green
                            LogEventLevel.Warning => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),   // Yellow
                            LogEventLevel.Error => new Vector4(1.0f, 0.4f, 0.4f, 1.0f),     // Red
                            LogEventLevel.Fatal => new Vector4(1.0f, 0.0f, 0.0f, 1.0f),     // Bright Red
                            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                        };

                        ImGui.PushStyleColor(ImGuiCol.Text, levelColor);
                        ImGui.TextUnformatted(log.Level.ToString());
                        ImGui.PopStyleColor();

                        // Message column (extract clean message without timestamp)
                        ImGui.TableNextColumn();
                        string cleanMessage = ExtractMessageOnly(log.Message);
                        ImGui.TextWrapped(cleanMessage);

                        // Tooltip with full message on hover
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                            ImGui.TextUnformatted($"{log.Timestamp:HH:mm:ss.fff}\n{log.Message}");
                            if (log.Exception != null)
                            {
                                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), $"\nException: {log.Exception.Message}");
                            }
                            ImGui.PopTextWrapPos();
                            ImGui.EndTooltip();
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }
        ImGui.End();
    }

    /// <summary>
    /// Extract short system name from full namespace
    /// </summary>
    private string GetShortSystemName(string fullName)
    {
        // Extract class name from namespace (e.g., "AlinasRailTools.Editor.ECS.Systems.LODSystem" -> "LODSystem")
        var parts = fullName.Split('.');
        return parts.Length > 0 ? parts[^1] : fullName;
    }

    /// <summary>
    /// Extract just the message part, removing timestamp and level prefix
    /// </summary>
    private string ExtractMessageOnly(string fullMessage)
    {
        // Format is typically: "[HH:mm:ss LVL] Message"
        var bracketEnd = fullMessage.IndexOf(']');
        if (bracketEnd > 0 && bracketEnd < fullMessage.Length - 1)
        {
            return fullMessage.Substring(bracketEnd + 2).Trim();
        }
        return fullMessage;
    }
}
