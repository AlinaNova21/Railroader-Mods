using System.Linq.Expressions;
using Core;
using HarmonyLib;
using MapEditor.Dialogs;
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
      _logger.Information("SelectedNodeChanged: " + (trackNode?.id ?? "<null>"));

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
      _logger.Information("SelectedSegmentChanged: " + (trackSegment?.id ?? "<null>"));

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
        _logger.Information("PrefixChanged: " + value);
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

    private static readonly ILogger _logger = Log.ForContext(typeof(EditorContext));

    private static string? _MixintoFile;

    public static void OpenMixinto(string fileName)
    {
      _MixintoFile = fileName;
      _logger.Information("Opening patch: {fileName}", fileName);
      PatchEditor = new PatchEditor(fileName);
      TrackSegmentDialog.Activate();
    }

    public static void CloseMixinto()
    {
      _logger.Information("Closing patch");
      ChangeManager.UndoAll();
      PatchEditor = null!;
      ChangeManager.Clear();
      TrackNodeDialog.CloseWindow();
      TrackSegmentDialog.CloseWindow();
      SelectedNode = null;
      SelectedSegment = null;

      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }

    public static void Save()
    {
      if (PatchEditor != null)
      {
        _logger.Information("Saving patch");
        PatchEditor.Save();
        PatchEditor = new PatchEditor(_MixintoFile!);
        ChangeManager.UndoAll();
      }
    }

    #region MapEditorDialog

    private static MapEditorDialog? _EditorDialog;
    public static MapEditorDialog MapEditorDialog => _EditorDialog ??= new MapEditorDialog();

    #endregion

    #region MapEditorDialog

    private static KeyboardSettingsDialog? _KeyboardSettingsDialog;
    public static KeyboardSettingsDialog KeyboardSettingsDialog => _KeyboardSettingsDialog ??= new KeyboardSettingsDialog();

    #endregion

    #region TrackNodeDialog

    private static TrackNodeDialog? _TrackNodeDialog;
    public static TrackNodeDialog TrackNodeDialog => _TrackNodeDialog ??= new TrackNodeDialog();

    #endregion

    #region TrackNodeDialog

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

  }
}
