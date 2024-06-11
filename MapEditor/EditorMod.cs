using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Railloader;
using RLD;
using Serilog;
using UI;
using UI.Builder;
using UI.Common;
using UI.Menu;
using UI.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MapEditor
{
  internal class EditorMod : SingletonPluginBase<EditorMod>, IUpdateHandler, IModTabHandler
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
      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
      Messenger.Default.Register(this, new Action<MapDidUnloadEvent>(OnMapDidUnload));
    }


    public override void OnDisable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod.Editor");
      harmony.UnpatchAll();
      Messenger.Default.Unregister(this);
    }

    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      try
      {
        _logger.Debug("OnMapDidLoad()");
        var editor = new GameObject("Editor");
        var parent = GameObject.Find("World").transform;
        if (parent != null)
        {
          _logger.Debug("Found World object");
          editor.transform.SetParent(parent);
        }
        else
        {
          _logger.Error("Could not find World object");
        }

        editor.AddComponent<Editor>();
        editor.SetActive(EditorContext.Settings.Enabled);

        AddButtonToTopRightArea();

        var scene = SceneDescriptor.Editor.LoadAsync(LoadSceneMode.Additive);
        scene.completed += _ => GameObject.Find("Definition Editor Mode Controller")?.SetActive(false);
      }
      catch (Exception e)
      {
        _logger.Debug(e.Message);
        _logger.Debug(e.StackTrace);
      }
    }

    private void OnMapDidUnload(MapDidUnloadEvent obj)
    {
      EditorContext.CloseMixinto();
    }

    private void AddButtonToTopRightArea()
    {
      var topRightArea = UnityEngine.Object.FindObjectOfType<TopRightArea>();
      if (topRightArea == null)
      {
        return;
      }

      var strip = topRightArea.transform.Find("Strip");
      var gameObject = new GameObject("MapEditorButton");
      gameObject.transform.parent = strip;
      gameObject.transform.SetSiblingIndex(9);

      var button = gameObject.AddComponent<Button>();
      button.onClick.AddListener(EditorContext.MapEditorDialog.ShowWindow);

      var image = gameObject.AddComponent<Image>();
      image.sprite = Sprite.Create(Resources.Icons.ConstructionIcon, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f));
      image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
      image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
    }

    public void Update()
    {
      var editor = UnityEngine.Object.FindObjectOfType<Editor>(true);
      if (editor != null && EditorContext.Settings.Enabled != editor.isActiveAndEnabled)
      {
        editor.gameObject.SetActive(EditorContext.Settings.Enabled);
      }
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
}
