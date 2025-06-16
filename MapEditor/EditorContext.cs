using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AlinasMapMod.Loaders;
using AlinasMapMod.MapEditor;
using Core;
using HarmonyLib;
using Helpers;
using MapEditor.Dialogs;
using MapEditor.Helpers;
using MapEditor.Managers;
using Railloader;
using RLD;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapEditor;

public class EditorScaling
{
  public int Movement { get; set; } = 100;
  public int Rotation { get; set; } = 1;
  public int Scale { get; set; } = 100;
}

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
    set {
      if (_SelectedNode != value) {
        _SelectedNode = value;
        OnSelectedNodeChanged(value);
      }
    }
  }

  private static TrackNode? _PreviousNode;

  private static void OnSelectedNodeChanged(TrackNode? trackNode)
  {
    _Logger.Information("SelectedNodeChanged: " + (trackNode?.id ?? "<null>"));

    if (PatchEditor == null) {
      _Logger.Information("PatchEditor NULL");
      return;
    }

    if (trackNode == null) {
      KeyboardManager.Deactivate();
      TrackNodeDialog.CloseWindow();
    } else {
      KeyboardManager.Activate();
      TrackNodeDialog.ShowWindow($"Node Editor - {trackNode.id}");

      if (Input.GetKey(KeyCode.LeftShift) && _PreviousNode != null) {
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
    set {
      if (_SelectedSegment != value) {
        _SelectedSegment = value;
        OnSelectedSegmentChanged(value);
      }
    }
  }

  private static void OnSelectedSegmentChanged(TrackSegment? trackSegment)
  {
    _Logger.Information("SelectedSegmentChanged: " + (trackSegment?.id ?? "<null>"));

    if (PatchEditor == null) {
      _Logger.Information("PatchEditor NULL");
      return;
    }

    if (trackSegment == null) {
      TrackSegmentDialog.CloseWindow();
      KeyboardManager.Deactivate();
    } else {
      TrackSegmentDialog.ShowWindow($"Segment Editor - {trackSegment.id}");
      KeyboardManager.Activate();
    }
  }

  #endregion

  #region SelectedScenery
  private static SceneryAssetInstance? _SelectedScenery;
  public static SceneryAssetInstance? SelectedScenery
  {
    get => _SelectedScenery;
    set {
      if (_SelectedScenery != value) {
        _SelectedScenery = value;
        OnSelectedSceneryChanged(value);
      }
    }
  }

  private static void OnSelectedSceneryChanged(SceneryAssetInstance? scenery)
  {
    _Logger.Information("SelectedSceneryChanged: " + (scenery?.name ?? "<null>"));
    if (PatchEditor == null) {
      _Logger.Information("PatchEditor NULL");
      return;
    }
    if (scenery == null) {
      SceneryDialog.CloseWindow();
      KeyboardManager.Deactivate();
    } else {
      SceneryDialog.ShowWindow($"Scenery Editor - {scenery.name}");
      KeyboardManager.Activate();
    }
  }
  #endregion

  #region SelectedObject
  private static List<IEditableObject> _SelectedObjects = [];
  private static IEditableObject? _selectedObject => _SelectedObjects.FirstOrDefault();
  public static bool HasSelectedObjects => _SelectedObjects.Any();
  public static List<IEditableObject> SelectedObjects
  {
    get => _SelectedObjects;
    set {
      _SelectedObjects = value;
      OnSelectedObjectsChanged();
    }
  }
  public static void AddSelectedObject(IEditableObject obj)
  {
    if (obj == null) return;
    if (!_SelectedObjects.Contains(obj)) {
      _SelectedObjects.Add(obj);
      OnSelectedObjectsChanged();
    }
  }

  public static void RemoveSelectedObject(IEditableObject obj)
  {
    if (obj == null) return;
    if (_SelectedObjects.Contains(obj)) {
      _SelectedObjects.Remove(obj);
      OnSelectedObjectsChanged();
    }
  }

  public static IEditableObject? SelectedObject
  {
    get => _selectedObject;
    set {
      if (_selectedObject != value) {
        if (value == null) {
          _SelectedObjects.Clear();
        } else if (!_SelectedObjects.Contains(value)) {
          _SelectedObjects.Clear();
          _SelectedObjects.Add(value);
        }
        OnSelectedObjectsChanged();
      }
    }
  }
  public static ITransformableObject? SelectedTransformableObject => _selectedObject as ITransformableObject;
  public static List<ITransformableObject> SelectedTransformableObjects => _SelectedObjects.OfType<ITransformableObject>().ToList();

  private static void OnSelectedObjectsChanged()
  {
    if (PatchEditor == null) {
      _Logger.Information("PatchEditor NULL");
      return;
    }
    if (_SelectedObjects.Any()) {
      var items = string.Join(", ", _SelectedObjects.Select(o => o.Id));
      ObjectDialog.ShowWindow($"Object Editor - {items}");
      KeyboardManager.Activate();
    } else {
      ObjectDialog.CloseWindow();
      KeyboardManager.Deactivate();
    }
  }
  #endregion

  #region ID Generators

  private static string _Prefix = "Custom_";

  public static string Prefix
  {
    get => _Prefix;
    set {
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

  private static IdGenerator? _LoaderIdGenerator;
  public static IdGenerator LoaderIdGenerator => _LoaderIdGenerator ??= _IdGeneratorFactory("L" + _Prefix, 4);

  private static IdGenerator? _SceneryIdGenerator;
  public static IdGenerator SceneryIdGenerator => _SceneryIdGenerator ??= _IdGeneratorFactory("Z" + _Prefix, 4);

  public static IdGenerator? _ObjectIdGenerator;
  public static IdGenerator ObjectIdGenerator => _ObjectIdGenerator ??= _IdGeneratorFactory("O" + _Prefix, 4);

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
  public static bool HasAlinasMapMod { get; private set; }
  public static ChangeManager ChangeManager { get; } = new ChangeManager();

  private static readonly ILogger _Logger = Log.ForContext(typeof(EditorContext));

  private static string? _MixintoFile;
  public static EditorScaling Scaling { get; set; } = new EditorScaling();
  public static void OpenMixinto(string fileName)
  {
    _MixintoFile = fileName;
    _Logger.Information("Opening patch: {fileName}", fileName);

    HasAlinasMapMod = ModdingContext.Mods.Any(m => m.Id == "AlinaNova21.AlinasMapMod" && m.IsLoaded);
    _Logger.Information("HasAlinasMapMod: " + HasAlinasMapMod);

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
    if (_TrackNodeDialog != null) {
      _TrackNodeDialog.CloseWindow();
    }
    if (_TrackSegmentDialog != null) {
      _TrackSegmentDialog.CloseWindow();
    }
    if (_SceneryDialog != null) {
      _SceneryDialog.CloseWindow();
    }
    SelectedNode = null;
    SelectedSegment = null;
    SelectedScenery = null;

    if (Graph.Shared != null)
      Graph.Shared.RebuildCollections();

    TrackObjectManager.Instance.Rebuild();

    DestroyUiHelpers();
  }

  public static void Save()
  {
    if (PatchEditor != null) {
      _Logger.Information("Saving patch");
      PatchEditor.Save();
      var console = GameObject.FindObjectOfType<UI.Console.Console>();
      console.AddLine($"Saved patch to {_MixintoFile}");

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

  #region SceneryDialog
  private static SceneryDialog? _SceneryDialog;
  public static SceneryDialog SceneryDialog => _SceneryDialog ??= new SceneryDialog();
  #endregion

  #region ObjectDialog
  private static ObjectDialog? _ObjectDialog;
  public static ObjectDialog ObjectDialog => _ObjectDialog ??= new ObjectDialog();
  #endregion

  public static void MoveCameraToSelectedNode()
  {
    if (SelectedNode != null) {
      CameraSelector.shared.ZoomToPoint(SelectedNode.transform.localPosition);
    }
  }

  public static void MoveCameraToSelectedSegment()
  {
    if (SelectedSegment != null) {
      var a = SelectedSegment.a.transform.localPosition;
      var b = SelectedSegment.b.transform.localPosition;
      var c = (a + b) / 2;
      CameraSelector.shared.ZoomToPoint(c);
    }
  }
  public static void MoveCameraToSelectedScenery()
  {
    if (SelectedScenery != null) {
      CameraSelector.shared.ZoomToPoint(SelectedScenery.transform.localPosition);
    }
  }

  public static void MoveCameraToSelectedObject()
  {
    if (SelectedTransformableObject != null) {
      CameraSelector.shared.ZoomToPoint(SelectedTransformableObject.Position);
    }
  }

  public static void UpdateUiHelpers()
  {
    CreateUiHelpers();
  }

  #region UI helpers

  private static void CreateUiHelpers()
  {
    foreach (var trackNode in Graph.Shared.Nodes!) {
      AttachUiHelper(trackNode);
    }

    foreach (var trackSegment in Graph.Shared.Segments!) {
      AttachUiHelper(trackSegment);
    }

    if (false && HasAlinasMapMod) {
      var loaders = GameObject.FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var loader in loaders) {
        AttachUiHelper(loader);
      }
    }

    foreach (var scenery in GameObject.FindObjectsByType<SceneryAssetInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
      AttachUiHelper(scenery);
    }

    foreach (ITransformableObject obj in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ITransformableObject>()) {
      AttachUiHelper(obj);
    }
  }

  private static void DestroyUiHelpers()
  {
    foreach (var trackNode in Graph.Shared.Nodes!) {
      var helper = trackNode.transform.Find("TrackNodeHelper");
      if (helper != null) {
        Object.Destroy(helper.gameObject);
      }
    }

    foreach (var trackSegment in Graph.Shared.Segments!) {
      var helper = trackSegment.transform.Find("TrackSegmentHelper");
      if (helper != null) {
        Object.Destroy(helper.gameObject);
      }
    }

    if (false && HasAlinasMapMod) {
      var loaders = GameObject.FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var loader in loaders) {
        var helper = loader.transform.Find("LoaderHelper");
        if (helper != null) {
          Object.Destroy(helper.gameObject);
        }
      }
    }

    foreach (var scenery in GameObject.FindObjectsByType<SceneryAssetInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
      var helper = scenery.transform.Find("SceneryHelper");
      if (helper != null) {
        Object.Destroy(helper.gameObject);
      }
    }

    foreach (ITransformableObject obj in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ITransformableObject>()) {
      var helper = obj.Transform.Find("ObjectHelper");
      if (helper != null) {
        Object.Destroy(helper.gameObject);
      }
    }
  }

  internal static void AttachUiHelper(TrackNode node)
  {
    var helper = node.GetComponentInChildren<TrackNodeHelper>();
    if (helper != null) {
      return;
    }
    var gameObject = new GameObject("TrackNodeHelper");
    gameObject.transform.SetParent(node.transform);
    gameObject.AddComponent<TrackNodeHelper>();
  }

  internal static void AttachUiHelper(TrackSegment segment)
  {
    var helper = segment.GetComponentInChildren<TrackSegmentHelper>();
    if (helper != null) {
      return;
    }
    var gameObject = new GameObject("TrackSegmentHelper");
    gameObject.transform.SetParent(segment.transform);
    gameObject.AddComponent<TrackSegmentHelper>();
  }

  internal static void AttachUiHelper(SceneryAssetInstance scenery)
  {
    if (scenery.transform.Find("SceneryHelper") != null) {
      GameObject.Destroy(scenery.transform.Find("SceneryHelper"));
    }
    var gameObject = new GameObject("SceneryHelper");
    gameObject.transform.SetParent(scenery.transform);
    gameObject.AddComponent<SceneryHelper>();
  }

  internal static void AttachUiHelper(ITransformableObject obj)
  {
    if (obj.Transform.Find("ObjectHelper") != null) {
      GameObject.Destroy(obj.Transform.Find("ObjectHelper"));
    }
    var gameObject = new GameObject("ObjectHelper");
    gameObject.transform.SetParent(obj.Transform);
    gameObject.AddComponent<ObjectHelper>();
  }
  #endregion

}
