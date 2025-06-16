using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AlinasMapMod.MapEditor;
using Helpers;
using MapEditor.Managers;
using Serilog;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs;

public sealed class MapEditorDialog : DialogBase
{
  public MapEditorDialog() : base("mapeditor", "Map Editor", 400, 800, Window.Position.UpperLeft)
  {
    _Sources = EditorContext.ModdingContext.GetMixintos("game-graph")
      .GroupBy(o => o.Source.ToString(), o => o.Mixinto)
      .ToDictionary(o => o.Key, o => o.ToList());

    _Mods = _Sources.Keys.ToList();
    _Mods.Sort();
    _Mods.Insert(0, "Select ...");
    _Factories = [];
  }

  private readonly Serilog.ILogger logger = Log.ForContext<MapEditorDialog>();

  private readonly Dictionary<string, List<string>> _Sources;
  private readonly List<string> _Mods;
  private int _SelectedMod;
  private int _SelectedPatch;

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Mod", builder.AddDropdown(_Mods, _SelectedMod, o => {
      _SelectedMod = o;
      _SelectedPatch = 0;
      EditorContext.CloseMixinto();
      builder.Rebuild();
    })!);

    if (_SelectedMod != 0) {
      var mod = _Mods[_SelectedMod];
      var graphs = _Sources[mod]!.Select(Path.GetFileNameWithoutExtension).ToList();
      graphs.Sort();
      graphs.Insert(0, "Select ...");

      // show second dropdown only if there is more than one graph ...
      if (graphs.Count > 2) {
        builder.AddField("Graph", builder.AddDropdown(graphs, _SelectedPatch, o => {
          _SelectedPatch = o;
          builder.Rebuild();
        })!);
      } else {
        _SelectedPatch = 1;
      }

      if (_SelectedPatch != 0) {
        var selected = graphs[_SelectedPatch];
        var mixinto = _Sources[mod]!.FirstOrDefault(m => Path.GetFileNameWithoutExtension(m) == selected);
        if (mixinto != null) {
          EditorContext.OpenMixinto(mixinto);
        } else {
          logger.Error($"Mixinto for mod {mod} and graph {selected} not found.");
        }
        //EditorContext.OpenMixinto(_Sources[mod]![_SelectedPatch - 1]!); // -1 because of 'Select ...'

        // IDEA: use $"{mod}_{graphs[_SelectedPatch]}" as prefix? (so player can tell which mod modified what)
        builder.AddField("Prefix", builder.AddInputField(EditorContext.Prefix, value => EditorContext.Prefix = value, "Prefix")!);
        builder.AddField("Recorded commands", () => EditorContext.ChangeManager.Count.ToString(), UIPanelBuilder.Frequency.Periodic);
        //builder.AddButton("Create Loader", () =>
        //{
        //  var position = CameraSelector.shared.CurrentCameraGroundPosition;
        //  position -= GameObject.Find("World").transform.position;
        //  LoaderManager.AddLoader(position);
        //});
        builder.AddButton("Create Scenery", () => {
          var position = CameraSelector.shared.CurrentCameraGroundPosition;
          position -= GameObject.Find("World").transform.position;
          SceneryManager.AddScenery(position);
        });
        foreach (var factory in Factories) {
          if (!factory.Enabled) continue;
          builder.AddButton($"Create {factory.Name}", () => {
            var position = CameraSelector.shared.CurrentCameraGroundPosition;
            position -= GameObject.Find("World").transform.position;
            var id = EditorContext.ObjectIdGenerator.Next();
            var type = factory.ObjectType;
            ObjectManager.Create(type, id, position);
          });
        }
        builder.HStack(stack => {
          stack.AddButtonCompact("Undo", EditorContext.ChangeManager.Undo);
          stack.AddButtonCompact("Redo", EditorContext.ChangeManager.Redo);
          stack.AddButtonCompact("Save", EditorContext.Save);
        });
        BuildSearch(builder);
      }
    }

    builder.AddButton("Rebuild Track", () => {
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    });

    builder.AddSection("Settings", BuildSettings);
    builder.AddExpandingVerticalSpacer();
  }

  private List<IObjectFactory> _Factories;
  private List<IObjectFactory> Factories
  {
    get {
      if (_Factories.Count == 0) {
        logger.Information("Loading factories");
        var list = new List<IObjectFactory>();
        foreach (var mod in EditorContext.ModdingContext.Mods) {
          if (mod.IsEnabled == false) {
            continue;
          }
          if (mod.Plugins == null) {
            continue;
          }
          logger.Debug($"Checking mod {mod.Name}");
          var assemblies = new HashSet<Assembly>();
          foreach (var plugin in mod.Plugins)
            assemblies.Add(plugin.GetType().Assembly);

          foreach (var assembly in assemblies) {
            var factories = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IObjectFactory).IsAssignableFrom(t))
              .Select(t => (IObjectFactory)Activator.CreateInstance(t))
              .ToList();
            if (factories.Count == 0) continue;
            logger.Debug($"Found {factories.Count} factories in mod {mod.Name} assembly {assembly.GetName()}");
            list.AddRange(factories);
          }
        }
        _Factories = list;
      }
      return _Factories;
    }
  }

  private List<SearchResult> searchResults = [];
  private string searchTerm = "";

  private struct SearchResult
  {
    public string id;
    public string display;
    public Type type;
  }

  private void DoSearch(string search)
  {
    searchResults.Clear();
    if (search.Length == 0) return;
    var nodes = Graph.Shared.Nodes.Where(n => n.id.Contains(search)).ToList();
    var segments = Graph.Shared.Segments.Where(n => n.id.Contains(search)).ToList();
    var sceneries = GameObject.FindObjectsOfType<SceneryAssetInstance>(true).Where(n => n.identifier.Contains(search));
    foreach (var node in nodes) {
      searchResults.Add(new SearchResult { id = node.id, display = $"Node {node.id}", type = typeof(TrackNode) });
    }
    foreach (var segment in segments) {
      searchResults.Add(new SearchResult { id = segment.id, display = $"Segment {segment.id}", type = typeof(TrackSegment) });
    }
    foreach (var scenery in sceneries) {
      searchResults.Add(new SearchResult { id = scenery.identifier, display = $"Scenery {scenery.identifier}", type = typeof(SceneryAssetInstance) });
    }
  }

  public void BuildSearch(UIPanelBuilder builder)
  {
    builder.AddField("Search", builder.AddInputField(searchTerm, value => {
      searchTerm = value;
      DoSearch(searchTerm);
      builder.Rebuild();
    }, "Search")!);

    builder.VScrollView(builder => {
      foreach (var sr in searchResults) {
        builder.AddButton(sr.display, () => {
          if (sr.type == typeof(TrackNode)) {
            EditorContext.SelectedNode = Graph.Shared.GetNode(sr.id);
            EditorContext.MoveCameraToSelectedNode();
          } else if (sr.type == typeof(TrackSegment)) {
            var s = EditorContext.SelectedSegment = Graph.Shared.GetSegment(sr.id);
            EditorContext.MoveCameraToSelectedSegment();
          } else if (sr.type == typeof(SceneryAssetInstance)) {
            EditorContext.SelectedScenery = GameObject.FindObjectsOfType<SceneryAssetInstance>().First(s => s.identifier == sr.id);
            EditorContext.MoveCameraToSelectedScenery();
          }
        });
      }
    });
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
