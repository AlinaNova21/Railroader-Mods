using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapEditor.Managers;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs
{
  public sealed class MapEditorDialog : DialogBase
  {
    public MapEditorDialog() : base("mapeditor", "Map Editor", 400, 350, Window.Position.UpperLeft)
    {
      _Sources = EditorContext.ModdingContext.GetMixintos("game-graph")
        .GroupBy(o => o.Source.ToString(), o => o.Mixinto)
        .ToDictionary(o => o.Key, o => o.ToList());

      _Mods = _Sources.Keys.ToList();
      _Mods.Insert(0, "Select ...");
    }

    private readonly Dictionary<string, List<string>> _Sources;
    private readonly List<string> _Mods;
    private int _SelectedMod;
    private int _SelectedPatch;

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Mod", builder.AddDropdown(_Mods, _SelectedMod, o =>
      {
        _SelectedMod = o;
        _SelectedPatch = 0;
        EditorContext.CloseMixinto();
        builder.Rebuild();
      })!);

      if (_SelectedMod != 0)
      {
        var mod = _Mods[_SelectedMod];
        var graphs = _Sources[mod]!.Select(Path.GetFileNameWithoutExtension).ToList();
        graphs.Insert(0, "Select ...");

        // show second dropdown only if there is more than one graph ...
        if (graphs.Count > 2)
        {
          builder.AddField("Graph", builder.AddDropdown(graphs, _SelectedPatch, o =>
          {
            _SelectedPatch = o;
            builder.Rebuild();
          })!);
        }
        else
        {
          _SelectedPatch = 1;
        }

        if (_SelectedPatch != 0)
        {
          EditorContext.OpenMixinto(_Sources[mod]![_SelectedPatch - 1]!); // -1 because of 'Select ...'

          // IDEA: use $"{mod}_{graphs[_SelectedPatch]}" as prefix? (so player can tell which mod modified what)
          builder.AddField("Prefix", builder.AddInputField(EditorContext.Prefix, value => EditorContext.Prefix = value, "Prefix")!);
          builder.AddField("Recorded commands", EditorContext.ChangeManager.Count.ToString, UIPanelBuilder.Frequency.Periodic);
          builder.AddButton("Create Loader", () =>
          {
            var position = CameraSelector.shared.CurrentCameraGroundPosition;
            position -= GameObject.Find("World").transform.position;
            LoaderManager.AddLoader(position);
          }); builder.AddButton("Create Scenery", () =>
          {
            var position = CameraSelector.shared.CurrentCameraGroundPosition;
            position -= GameObject.Find("World").transform.position;
            SceneryManager.AddScenery(position);
          });
          builder.HStack(stack =>
          {
            stack.AddButtonCompact("Undo", EditorContext.ChangeManager.Undo);
            stack.AddButtonCompact("Redo", EditorContext.ChangeManager.Redo);
            stack.AddButtonCompact("Save", EditorContext.Save);
          });
        }
      }

      builder.AddButton("Rebuild Track", () =>
      {
        Graph.Shared.RebuildCollections();
        TrackObjectManager.Instance.Rebuild();
      });

      builder.AddSection("Settings", BuildSettings);
      builder.AddExpandingVerticalSpacer();
    }

    public void BuildSettings(UIPanelBuilder builder)
    {
      builder.AddButton("Keyboard bindings", EditorContext.KeyboardSettingsDialog.ShowWindow);
    }

    protected override void AfterWindowClosed()
    {
      EditorContext.SaveSettings();
    }

  }
}
