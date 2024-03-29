using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core;
using Effects;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using MapEditor.Tools;
using Railloader;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UI.CompanyWindow;
using UI.Map;
using UI.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MapEditor
{
  class EditorMod : SingletonPluginBase<EditorMod>, IUpdateHandler, IModTabHandler
  {
    public Settings Settings { get; }
    private IModdingContext context;
    private IUIHelper uiHelper;
    public IUIHelper UIHelper => uiHelper;
    private Serilog.ILogger logger = Log.ForContext<EditorMod>();
    private PatchEditor patchEditor { get; set; }

    public EditorMod(IModdingContext _context, IUIHelper _uiHelper)
    {
      context = _context;
      uiHelper = _uiHelper;
      Settings = context.LoadSettingsData<Settings>("AlinaNova21.MapEditor") ?? new Settings();
      // patchEditor = new PatchEditor();
    }

    public override void OnEnable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
      harmony.PatchAll();
      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
    }

    public override void OnDisable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
      harmony.UnpatchAll();
      Messenger.Default.Unregister(this);
    }

    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Debug("OnMapDidLoad()");
      var editor = new GameObject("Editor");
      var parent = GameObject.Find("World").transform;
      if (parent != null)
      {
        logger.Debug("Found World object");
        editor.transform.SetParent(parent);
      }
      else
      {
        logger.Error("Could not find World object");
      }
      editor.AddComponent<Editor>();
      editor.SetActive(Settings.Enabled);
      var window = uiHelper.CreateWindow(500, 500, UI.Common.Window.Position.CenterRight);
      window.Title = "Map Editor";
      uiHelper.PopulateWindow(window, ToolPanelDidOpen);
      window.OnShownDidChange += (shown) =>
      {
        if (!shown)
        {
          ToolPanelDidClose();
        }
      };
      var tr = UnityEngine.Object.FindObjectOfType<TopRightArea>();
      if (tr != null)
      {
        var buttons = tr.transform.Find("Buttons");
        var go = new GameObject("MapEditorButton");
        go.transform.parent = buttons;
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
          window.ShowWindow();
        });
        var rawIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapEditor.Resources.construction-icon.png");
        var iconData = new BinaryReader(rawIcon).ReadBytes((int)rawIcon.Length);
        var icon = go.AddComponent<Image>();
        var tex = new Texture2D(24, 24, TextureFormat.ARGB32, false);
        ImageConversion.LoadImage(tex, iconData);
        icon.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f));
        icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
        icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
      }
      var scene = SceneDescriptor.Editor.LoadAsync(LoadSceneMode.Additive);
      scene.completed += (op) =>
      {
        GameObject.Find("Definition Editor Mode Controller")?.SetActive(false);
      };
      // SceneManager.LoadScene(SceneDescriptor.Editor., LoadSceneMode.Additive);
    }

    public void ToolPanelDidOpen(UIPanelBuilder builder)
    {
      builder.AddButton("Reset Node Helpers", () =>
      {
        var nodeHelpers = GameObject.FindObjectOfType<NodeHelpers>();
        if (nodeHelpers != null)
        {
          nodeHelpers.Reset();
        }
      });
      // var baseDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Mods");
      var mixintos = context.GetMixintos("game-graph");
      var outerBuilder = builder;
      builder.AddSection("Editing", builder => {
        if (EditorContext.Instance == null) {
          foreach(var mixinto in mixintos) {
            builder.HStack(builder =>
            {
              builder.AddLabel(Path.GetFileName(Path.GetDirectoryName(mixinto.Mixinto)));
              builder.AddButtonCompact("Edit", () =>
              {
                logger.Debug("Editing {0}", mixinto.Mixinto);
                new EditorContext(mixinto.Mixinto);
                Tool.UIMoveTool.Activate();
                outerBuilder.Rebuild();
              });
            });
          }
        } else {
          builder.AddField("Prefix", builder.AddInputField(EditorContext.Instance?.Prefix ?? "", (value) =>
          {
            if (EditorContext.Instance != null)
            {
              EditorContext.Instance.Prefix = value;
            }
          }, "Prefix", null));
          builder.AddButtonCompact("Undo", () => EditorContext.Instance.ChangeManager.Undo());
          builder.AddButtonCompact("Redo", () => EditorContext.Instance.ChangeManager.Redo());
          builder.AddButton("Save", () => EditorContext.Instance?.Save());
        }
        builder.AddButton("Rebuild Track", () => {
          Graph.Shared.RebuildCollections();
          TrackObjectManager.Instance.Rebuild();
        });
      });
      builder.AddSection("Settings", ModTabDidOpen);
      builder.AddExpandingVerticalSpacer();
    }

    public void ToolPanelDidClose()
    {
      ModTabDidClose();
    }

    public void ModTabDidClose()
    {
      context.SaveSettingsData("AlinaNova21.MapEditor", Settings);
    }

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
      builder.AddSection("Map Editor", builder =>
      {
        builder.AddField("Enabled", builder.AddToggle(
          () => Settings.Enabled,
          (value) => Settings.Enabled = value
        ));

        builder.AddField("Show Node Helpers", builder.AddToggle(
          () => Settings.ShowNodeHelpers,
          (value) => Settings.ShowNodeHelpers = value
        ));
      });
    }

    public void Update()
    {
      var editor = UnityEngine.Object.FindObjectOfType<Editor>(true);
      if (editor && Settings.Enabled != editor.isActiveAndEnabled)
      {
        editor.gameObject.SetActive(Settings.Enabled);
      }
    }
  }
}
