using System;
using System.Linq;
using AlinasMapMod;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Helpers;
using Map.Runtime;
using Map.Runtime.MapModifiers;
using Railloader;
using Serilog;
using StrangeCustoms;
using UI;
using UI.Builder;
using UI.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MapEditor;

internal sealed class EditorMod : SingletonPluginBase<EditorMod>, IModTabHandler
{

  private readonly Serilog.ILogger _logger = Log.ForContext<EditorMod>();

  public EditorMod(IModdingContext _context, IUIHelper _uiHelper)
  {
    EditorContext.ModdingContext = _context;
    EditorContext.UIHelper = _uiHelper;
  }

  public override void OnEnable()
  {
    var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
    harmony.PatchAll();
    Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
    Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapDidUnload);
    Messenger.Default.Register<GraphDidChangeEvent>(this, OnGraphDidChange);
    Messenger.Default.Register<QueryTooltipUpdateEvent>(this, OnQueryTooltipUpdate);
  }

  private void OnQueryTooltipUpdate(QueryTooltipUpdateEvent @event)
  {
    if (!@event.Hit)
      return;
    var raycastHit = @event.RaycastHit;
    Vector3 vector = WorldTransformer.WorldToGame(raycastHit.point);
    var mapManager = MapManager.Instance;
    var pos = mapManager.TilePositionFromPoint(vector);
    var mods = mapManager.HeightmapModifiersOverlapping(pos).ToList();
    mods.Sort((HeightmapModifier a, HeightmapModifier b) => a.Order.CompareTo(b.Order));
    @event.AppendText("Height Masks: {0:F0}", mods.Count);
    var mods2 = (from mod in mods
                 group mod by mod.Order).ToList();
    mods2.Sort((IGrouping<int, HeightmapModifier> a, IGrouping<int, HeightmapModifier> b) => a.Key.CompareTo(b.Key));
    foreach (var grouping in mods2) {
      var cnt = grouping.Count();
      @event.AppendText("  {0:F0}: {1:F0} {2}", grouping.Key, cnt, cnt > 256 ? " > 256!" : "");
    }
    if (mods2.Any(g => g.Count() > 256)) {
      @event.AppendText("  > 256 height masks in a group!");
      @event.AppendText("  Terrain masks may not apply.");
    }
  }

  private void OnGraphDidChange(GraphDidChangeEvent @event)
  {
    if (EditorContext.PatchEditor != null) {
      EditorContext.UpdateUiHelpers();
    }
  }

  public override void OnDisable()
  {
    var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
    harmony.UnpatchAll();
    Messenger.Default.Unregister(this);
  }

  private void OnMapDidLoad(MapDidLoadEvent @event)
  {
    try {
      _logger.Debug("OnMapDidLoad()");
      var editor = new GameObject("Editor");
      var parent = GameObject.Find("World");
      if (parent != null) {
        _logger.Debug("Found World object");
        editor.transform.SetParent(parent.transform);
      } else {
        _logger.Error("Could not find World object");
      }

      editor.AddComponent<Editor>();
      editor.SetActive(true);

      AddButtonToTopRightArea();

      var scene = SceneDescriptor.Editor.LoadAsync(LoadSceneMode.Additive);
      if (scene != null) {
        scene.completed += _ => GameObject.Find("Definition Editor Mode Controller")?.SetActive(false);
      }
    } catch (Exception e) {
      _logger.Debug(e.Message!);
      _logger.Debug(e.StackTrace!);
    }
  }

  private void OnMapDidUnload(MapDidUnloadEvent obj)
  {
    // EditorContext.CloseMixinto();
  }

  private void AddButtonToTopRightArea()
  {
    var topRightArea = UnityEngine.Object.FindObjectOfType<TopRightArea>();
    if (topRightArea == null) {
      return;
    }

    var strip = topRightArea.transform.Find("Strip");
    if (strip == null) {
      return;
    }

    var gameObject = new GameObject("MapEditorButton")
    {
      transform = { parent = strip }
    };
    gameObject.transform.SetSiblingIndex(9);

    var button = gameObject.AddComponent<Button>();
    button.onClick.AddListener(EditorContext.MapEditorDialog.ShowWindow);

    var image = gameObject.AddComponent<Image>();
    image.sprite = Sprite.Create(Resources.Icons.ConstructionIcon, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f))!;
    image.rectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
    image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
  }

  public void ModTabDidOpen(UIPanelBuilder builder)
  {
    EditorContext.MapEditorDialog.BuildSettings(builder);
  }

  public void ModTabDidClose()
  {
    EditorContext.SaveSettings();
  }
}
