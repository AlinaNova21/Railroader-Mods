using System;
using System.Collections;
using System.IO;
using System.Reflection;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Helpers;
using Railloader;
using Serilog;
using UI;
using UI.Builder;
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

    private Serilog.ILogger logger = Log.ForContext<EditorMod>();
    
    public EditorMod(IModdingContext _context, IUIHelper _uiHelper)
    {
      context = _context;
      uiHelper = _uiHelper;
      Settings = context.LoadSettingsData<Settings>("AlinaNova21.MapEditor") ?? new Settings();
      
    }


    public override void OnEnable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
      harmony.PatchCategory("AlinasMapMod.Editor");
      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
    }

    public override void OnDisable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
      harmony.UnpatchAll("AlinaNova21.AlinasMapMod.Editor");
      Messenger.Default.Unregister(this);
    }

    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Debug("OnMapDidLoad()");
      var editor = new GameObject("Editor");
      var parent = GameObject.Find("World").transform;
      if (parent != null) {
        logger.Debug("Found World object");
        editor.transform.SetParent(parent);
      } else {
        logger.Error("Could not find World object");
      }
      editor.AddComponent<Editor>();
      editor.SetActive(Settings.Enabled);
      var window = uiHelper.CreateWindow(200,200,UI.Common.Window.Position.CenterRight);
      window.Title = "Map Editor";
      uiHelper.PopulateWindow(window, builder => {
        ModTabDidOpen(builder);
        builder.AddButton("Reset Node Helpers", () => {
          var nodeHelpers = GameObject.FindObjectOfType<NodeHelpers>();
          if (nodeHelpers != null) {
            nodeHelpers.Reset();
          }
        });
        builder.AddExpandingVerticalSpacer();
      });
      var tr = UnityEngine.Object.FindObjectOfType<TopRightArea>();
      if (tr != null) {
        var buttons = tr.transform.Find("Buttons");
        var go = new GameObject("MapEditorButton");
        go.transform.parent = buttons;
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() => {
          window.ShowWindow();
        });
        var rawIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapEditor.Resources.construction-icon.png");
        var iconData = new BinaryReader(rawIcon).ReadBytes((int)rawIcon.Length);
        var icon = go.AddComponent<Image>();
        var tex = new Texture2D(24,24, TextureFormat.ARGB32, false);
        ImageConversion.LoadImage(tex, iconData);
        icon.sprite = Sprite.Create(tex, new Rect(0,0,24,24), new Vector2(0.5f, 0.5f));
        icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
        icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
      }
      var scene = SceneDescriptor.Editor.LoadAsync(LoadSceneMode.Additive);
      scene.completed += (op) => {
        GameObject.Find("Definition Editor Mode Controller")?.SetActive(false);
      };
      // SceneManager.LoadScene(SceneDescriptor.Editor., LoadSceneMode.Additive);
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
