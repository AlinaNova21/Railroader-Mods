using System.Linq.Expressions;
using Core;
using HarmonyLib;
using MapEditor.Dialogs;
using MapEditor.Helpers;
using MapEditor.Managers;
using Railloader;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapEditor
{
  public static class EditorContext
  {

    #region Settings

    private static Settings? _Settings;
    public static Settings Settings => _Settings ??= ModdingContext.LoadSettingsData<Settings>("AlinaNova21.MapEditor") ?? new Settings();

    public static void SaveSettings()
    {
      ModdingContext.SaveSettingsData("AlinaNova21.MapEditor", Settings);
    }

    #endregion

    #region SelectedNode

    private static TrackNode? _SelectedNode;

    public static TrackNode? SelectedNode
    {
      get => _SelectedNode;
      set
      {
        if (_SelectedNode != value)
        {
          _SelectedNode = value;
          OnSelectedNodeChanged(value);
        }
      }
    }

    private static TrackNode? _PreviousNode;

    private static void OnSelectedNodeChanged(TrackNode? trackNode)
    {
      _Logger.Information("SelectedNodeChanged: " + (trackNode?.id ?? "<null>"));

      if (PatchEditor == null)
      {
        _Logger.Information("PatchEditor NULL");
        return;
      }

      if (trackNode == null)
      {
        KeyboardManager.Deactivate();
        TrackNodeDialog.CloseWindow();
      }
      else
      {
        KeyboardManager.Activate();
        TrackNodeDialog.ShowWindow($"Node Editor - {trackNode.id}");

        if (Input.GetKey(KeyCode.LeftShift) && _PreviousNode != null)
        {
          NodeManager.ConnectNodes(_PreviousNode.id);
        }

        _PreviousNode = trackNode;
      }
    }

    #endregion

    #region SelectedSegment

    private static TrackSegment? _SelectedSegment;

    public static TrackSegment? SelectedSegment
    {
      get => _SelectedSegment;
      set
      {
        if (_SelectedSegment != value)
        {
          _SelectedSegment = value;
          OnSelectedSegmentChanged(value);
        }
      }
    }

    private static void OnSelectedSegmentChanged(TrackSegment? trackSegment)
    {
      _Logger.Information("SelectedSegmentChanged: " + (trackSegment?.id ?? "<null>"));

      if (PatchEditor == null)
      {
        _Logger.Information("PatchEditor NULL");
        return;
      }

      if (trackSegment == null)
      {
        TrackSegmentDialog.CloseWindow();
        KeyboardManager.Deactivate();
      }
      else
      {
        TrackSegmentDialog.ShowWindow($"Segment Editor - {trackSegment.id}");
        KeyboardManager.Activate();
      }
    }

    #endregion

    #region ID Generators

    private static string _Prefix = "Custom_";

    public static string Prefix
    {
      get => _Prefix;
      set
      {
        _Logger.Information("PrefixChanged: " + value);
        _Prefix = value;
        _TrackNodeIdGenerator = null;
        _TrackSegmentIdGenerator = null;
      }
    }

    private static IdGenerator? _TrackNodeIdGenerator;
    public static IdGenerator TrackNodeIdGenerator => _TrackNodeIdGenerator ??= _IdGeneratorFactory("N" + _Prefix, 4);

    private static IdGenerator? _TrackSegmentIdGenerator;
    public static IdGenerator TrackSegmentIdGenerator => _TrackSegmentIdGenerator ??= _IdGeneratorFactory("S" + _Prefix, 4);

    private delegate IdGenerator IdGeneratorFactoryDelegate(string prefix, int digits);

    private static readonly IdGeneratorFactoryDelegate _IdGeneratorFactory = BuildIdGeneratorFactory();

    private static IdGeneratorFactoryDelegate BuildIdGeneratorFactory()
    {
      var constructor = AccessTools.Constructor(typeof(IdGenerator), [typeof(string), typeof(int)])!;
      var prefix = Expression.Parameter(typeof(string), "prefix");
      var digits = Expression.Parameter(typeof(int), "digits");
      var instance = Expression.New(constructor, prefix, digits);
      var result = Expression.Convert(instance, typeof(IdGenerator));
      var lambda = Expression.Lambda<IdGeneratorFactoryDelegate>(result, prefix, digits);
      return lambda.Compile();
    }

    #endregion

    public static IModdingContext ModdingContext { get; set; } = null!;

    public static IUIHelper UIHelper { get; set; } = null!;

    public static PatchEditor? PatchEditor { get; private set; }

    public static ChangeManager ChangeManager { get; } = new ChangeManager();

    private static readonly ILogger _Logger = Log.ForContext(typeof(EditorContext));

    private static string? _MixintoFile;

    public static void OpenMixinto(string fileName)
    {
      _MixintoFile = fileName;
      _Logger.Information("Opening patch: {fileName}", fileName);

      SelectedNode = null;
      SelectedSegment = null;
      PatchEditor = new PatchEditor(fileName);
      ChangeManager.Clear();
      TrackSegmentDialog.Activate();

      CreateUiHelpers();
    }

    public static void CloseMixinto()
    {
      _Logger.Information("Closing patch");
      _Logger.Information("Removing unsaved (" + ChangeManager.Count + ") changes");

      ChangeManager.UndoAll();
      PatchEditor = null!;
      TrackNodeDialog.CloseWindow();
      TrackSegmentDialog.CloseWindow();
      SelectedNode = null;
      SelectedSegment = null;

      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();

      DestroyUiHelpers();
    }

    public static void Save()
    {
      if (PatchEditor != null)
      {
        _Logger.Information("Saving patch");
        PatchEditor.Save();

        PatchEditor = new PatchEditor(_MixintoFile!);
        ChangeManager.Clear();
      }
    }

    #region MapEditorDialog

    private static MapEditorDialog? _EditorDialog;
    public static MapEditorDialog MapEditorDialog => _EditorDialog ??= new MapEditorDialog();

    #endregion

    #region KeyboardSettingsDialog

    private static KeyboardSettingsDialog? _KeyboardSettingsDialog;
    public static KeyboardSettingsDialog KeyboardSettingsDialog => _KeyboardSettingsDialog ??= new KeyboardSettingsDialog();

    #endregion

    #region TrackNodeDialog

    private static TrackNodeDialog? _TrackNodeDialog;
    public static TrackNodeDialog TrackNodeDialog => _TrackNodeDialog ??= new TrackNodeDialog();

    #endregion

    #region TrackSegmentDialog

    private static TrackSegmentDialog? _TrackSegmentDialog;
    public static TrackSegmentDialog TrackSegmentDialog => _TrackSegmentDialog ??= new TrackSegmentDialog();

    #endregion

    public static void MoveCameraToSelectedNode()
    {
      if (SelectedNode != null)
      {
        CameraSelector.shared.ZoomToPoint(SelectedNode.transform.localPosition);
      }
    }

    public static void UpdateUiHelpers()
    {
      CreateUiHelpers();
    }

    #region UI helpers

    private static void CreateUiHelpers()
    {
      foreach (var trackNode in Graph.Shared.Nodes!)
      {
        AttachUiHelper(trackNode);
      }

      foreach (var trackSegment in Graph.Shared.Segments!)
      {
        AttachUiHelper(trackSegment);
      }
    }

    private static void DestroyUiHelpers()
    {
      foreach (var trackNode in Graph.Shared.Nodes!)
      {
        var helper = trackNode.transform.Find("TrackNodeHelper");
        if (helper != null)
        {
          Object.Destroy(helper.gameObject);
        }
      }

      foreach (var trackSegment in Graph.Shared.Segments!)
      {
        var helper = trackSegment.transform.Find("TrackSegmentHelper");
        if (helper != null)
        {
          Object.Destroy(helper.gameObject);
        }
      }

    }

    internal static void AttachUiHelper(TrackNode node)
    {
      if (node.transform.Find("TrackNodeHelper") != null)
      {
        return;
      }
      var gameObject = new GameObject("TrackNodeHelper");
      gameObject.transform.SetParent(node.transform);
      gameObject.AddComponent<TrackNodeHelper>();
    }

    internal static void AttachUiHelper(TrackSegment segment)
    {
      if (segment.transform.Find("TrackSegmentHelper") != null)
      {
        return;
      }
      var gameObject = new GameObject("TrackSegmentHelper");
      gameObject.transform.SetParent(segment.transform);
      gameObject.AddComponent<TrackSegmentHelper>();
    }
    #endregion

  }
}
