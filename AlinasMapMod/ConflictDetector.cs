#if DEBUG
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Railloader;
using Serilog;
using Serilog.Core;
using SimpleGraph.Runtime;
using StrangeCustoms.Tracks;
using Track;

namespace AlinasMapMod;

class ConflictDetector
{
  readonly IModdingContext _moddingContext;
  private ConflictData conflictData = new ConflictData();
  private ILogger logger = Log.ForContext<ConflictDetector>();

  public ConflictDetector(IModdingContext moddingContext)
  {
    _moddingContext = moddingContext;
  }
  public void CheckForConflicts()
  {
    var loadedNodes = UnityEngine.GameObject.FindObjectsOfType<TrackNode>();
    var loadedSegments = UnityEngine.GameObject.FindObjectsOfType<TrackSegment>();

    // Load all game-graph mixintos
    var mixintos = _moddingContext.GetMixintos("game-graph");
    foreach (var mixinto in mixintos)
    {
      string path;
      if (!_moddingContext.TryResolveFilePath(Path.GetDirectoryName(Assembly.GetAssembly(typeof(ConflictDetector)).Location), mixinto.Mixinto, false, out path))
      {
        logger.Warning($"Mixinto {mixinto} is not in the mods directory, skipping");
        continue;
      }
      var modName = mixinto.Source.Name;
      var modId = mixinto.Source.Id;
      conflictData.Mods[modId] = mixinto.Source;
      logger.Information($"Checking for conflicts in {modName} ({modId})");
      var patch = new PatchEditor(path);
      var nodes = patch.GetNodes();
      var segments = patch.GetSegments();
      foreach (var id in nodes.Keys)
      {
        var nodeData = conflictData.GetOrCreateNode(id);
        nodeData.Mods.Add(modId);
      }
      foreach (var id in segments.Keys)
      {
        var segmentData = conflictData.GetOrCreateSegment(id);
        segmentData.Mods.Add(modId);
        var segment = segments[id];
        var startId = segment.Value<string>("startId");
        var endId = segment.Value<string>("endId");
        // logger.Information(segment.ToString());
        if (startId != null)
        {
          segmentData.Nodes.Add(startId);
          var startNode = conflictData.GetOrCreateNode(startId);
          startNode.Segments.Add(id);
        }
        if (endId != null)
        {
          segmentData.Nodes.Add(endId);
          var endNode = conflictData.GetOrCreateNode(endId);
          endNode.Segments.Add(id);
        }
      }
      foreach (var id in patch.GetSpans().Keys)
      {
        var spanData = conflictData.GetOrCreateSpan(id);
        spanData.Mods.Add(modId);
      }
    }

    var data = JsonConvert.SerializeObject(conflictData, Formatting.Indented);
    File.WriteAllText("conflictData.json", data);
  }
}

class NodeConflictData
{
  public HashSet<string> Mods { get; } = [];
  public HashSet<string> Segments { get; } = [];
  public bool HasConflict { get => Segments.Count > 3; }
}

class SegmentConflictData
{
  public HashSet<string> Mods { get; } = [];
  public HashSet<string> Nodes { get; } = [];
  public bool HasConflict { get => Nodes.Count > 2; }
}

class SpanConflictData
{
  public HashSet<string> Mods { get; } = [];
  // public HashSet<string> Segments { get; } = [];
  public bool HasConflict { get => false; }
}

class ConflictData
{
  public Dictionary<string, IMod> Mods { get; } = new Dictionary<string, IMod>();
  public Dictionary<string, NodeConflictData> Nodes { get; set; } = new Dictionary<string, NodeConflictData>();
  public Dictionary<string, SegmentConflictData> Segments { get; set; } = new Dictionary<string, SegmentConflictData>();
  public Dictionary<string, SpanConflictData> Spans { get; set; } = new Dictionary<string, SpanConflictData>();

  public Dictionary<string, NodeConflictData> ConflictingNodes { get => Nodes.Where(x => x.Value.HasConflict).ToDictionary(x => x.Key, x => x.Value); }
  public Dictionary<string, SegmentConflictData> ConflictingSegments { get => Segments.Where(x => x.Value.HasConflict).ToDictionary(x => x.Key, x => x.Value); }
  public Dictionary<string, SpanConflictData> ConflictingSpans { get => Spans.Where(x => x.Value.HasConflict).ToDictionary(x => x.Key, x => x.Value); }

  public NodeConflictData GetOrCreateNode(string node)
  {
    if (!Nodes.ContainsKey(node)) {
      Nodes[node] = new NodeConflictData();
    }
    return Nodes[node];
  }

  public SegmentConflictData GetOrCreateSegment(string segment)
  {
    if (!Segments.ContainsKey(segment)) {
      Segments[segment] = new SegmentConflictData();
    }

    return Segments[segment];
  }

  public SpanConflictData GetOrCreateSpan(string span)
  {
    if (!Spans.ContainsKey(span)) {
      Spans[span] = new SpanConflictData();
    }
    return Spans[span];
  }
}

class VanillaMod : IMod
{
  public string Id => "vanilla";
  public string Name => "Base Game";

  public bool IsEnabled => true;

  public bool IsLoaded => true;

  public bool IsFaulted => false;

  public PluginBase[] Plugins => [];

  public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

  public string Directory => "";

  public string[] LoadBefore => [];

  public ModReference[] LoadAfter => [];

  public ModReference[] Requires => [];

  public ModReference[] ConflictsWith => [];
}

#endif
