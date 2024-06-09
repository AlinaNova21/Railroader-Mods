using System;
using Core;
using HarmonyLib;
using MapEditor.StateTracker;
using MapEditor.Tools;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine.SceneManagement;

namespace MapEditor
{
  using MapEditor.Managers;

  // note: this one do not know if it wants to be singleton or static class ... (static class makes more sense to me)
  public class EditorContext
  {
    public static EditorContext Instance { get; private set; }
    public PatchEditor PatchEditor { get; private set; }

    public ChangeManager ChangeManager { get; private set; } = new ChangeManager();

    public TrackNode? SelectedNode { get; set; }
    public TrackSegment? SelectedSegment { get; set; }

    public TrackNode HoveredNode { get; set; }
    public TrackSegment HoveredSegment { get; set; }

    private BaseTool activeTool;
    public BaseTool ActiveTool => activeTool;

    private string _prefix = "Custom_";
    public string Prefix
    {
      get => _prefix;
      set
      {
        _prefix = value;
        TrackNodeIdGenerator = NewIdGenerator($"N{value}", 4);
        TrackSegmentIdGenerator = NewIdGenerator($"S{value}", 4);
      }
    }
    public IdGenerator TrackNodeIdGenerator = NewIdGenerator($"NCustom_", 4);
    public IdGenerator TrackSegmentIdGenerator = NewIdGenerator($"SCustom_", 4);

    private static IdGenerator NewIdGenerator(string prefix, int digits = 4)
    {
      var cons = AccessTools.Constructor(typeof(IdGenerator), [typeof(string), typeof(int)]);
      return cons.Invoke([prefix, digits]) as IdGenerator;
    }

    #region Events
    public static event Action<EditorContext> EditorContextChanged;
    public static event Action<TrackNode> NodeSelectedChanged;
    public static event Action<TrackSegment> SegmentSelectedChanged;
    #endregion


    private static ILogger log = Log.ForContext<EditorContext>();

    public static void Unload()
    {
      try
      {
        Instance = null;
      }
      catch (Exception e)
      {
        
        log.Error(e, "Failed to unload editor context");
      }
    }

    public EditorContext(string filename)
    {
      Instance = this;
      PatchEditor = new PatchEditor(filename);
      EditorContextChanged?.Invoke(this);
    }

    public void SetActiveTool(BaseTool tool)
    {
      activeTool = tool;
      EditorContextChanged?.Invoke(this);
    }

    internal void Save()
    {
      PatchEditor.Save();
    }

    internal void SelectNode(TrackNode newNode)
    {
      log.Information("SelectNode: " + newNode?.id);
      SelectedNode = newNode;
      NodeSelectedChanged?.Invoke(newNode);

      if (newNode == null) {
        KeyboardManager.Deactivate();
      } else {
        KeyboardManager.Activate();
      }
    }

    internal void SelectSegment(TrackSegment newNode)
    {
      log.Information("SelectSegment: " + newNode?.id);
      SelectedSegment = newNode;
      SegmentSelectedChanged?.Invoke(newNode);
    }
  }
}
