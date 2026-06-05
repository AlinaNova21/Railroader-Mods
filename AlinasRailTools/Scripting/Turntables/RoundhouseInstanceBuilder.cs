using Helpers;
using KeyValue.Runtime;
using RollingStock.Controls;
using Serilog;
using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

public class RoundhouseInstanceBuilder : BaseBuilder<RoundhouseBuilder, RoundhouseInstanceBuilder>
{
  private readonly Serilog.ILogger logger = Log.ForContext<RoundhouseInstanceBuilder>();

  public RoundhouseInstance Roundhouse { get; }
  public Helper Helper => Parent.Helper;

  private int _lastStalls = -1; // Track last built stalls to avoid unnecessary rebuilds

  public RoundhouseInstanceBuilder(RoundhouseBuilder parent, RoundhouseInstance roundhouse)
    : base(parent, roundhouse.identifier)
  {
    Roundhouse = roundhouse;
  }

  public override RoundhouseBuilder Remove()
  {
    // No prefab instances to clean up - we instantiate fresh each time from templates
    return Parent.RemoveRoundhouse(Id);
  }

  public override RoundhouseInstanceBuilder Invalidate()
  {
    // Invalidate doesn't auto-rebuild - Build() should be called explicitly when needed
    // This prevents redundant builds when multiple properties are changed
    return this;
  }

  // Properties
  public int Subdivisions
  {
    get => Roundhouse.subdivisions;
    set => Roundhouse.subdivisions = value;
  }

  public int Stalls
  {
    get => Roundhouse.stalls;
    set => Roundhouse.stalls = value;
  }

  public int TrackLength
  {
    get => Roundhouse.trackLength;
    set => Roundhouse.trackLength = value;
  }

  public GameObject StallTemplate
  {
    get => Roundhouse.stallTemplate;
    set => Roundhouse.stallTemplate = value;
  }

  public GameObject SideTemplate
  {
    get => Roundhouse.sideTemplate;
    set => Roundhouse.sideTemplate = value;
  }

  public GameObject BetweenTemplate
  {
    get => Roundhouse.betweenTemplate;
    set => Roundhouse.betweenTemplate = value;
  }

  public TurntableInstance Turntable
  {
    get => Roundhouse.turntable;
    set => Roundhouse.turntable = value;
  }

  public Vector3 Position
  {
    get => Roundhouse.transform.localPosition;
    set => Roundhouse.transform.localPosition = value;
  }

  public Quaternion Rotation
  {
    get => Roundhouse.transform.localRotation;
    set => Roundhouse.transform.localRotation = value;
  }

  public Vector3 Euler
  {
    get => Roundhouse.transform.localEulerAngles;
    set => Roundhouse.transform.localEulerAngles = value;
  }

  public Vector3 EulerAngles
  {
    get => Roundhouse.transform.localEulerAngles;
    set => Roundhouse.transform.localEulerAngles = value;
  }

  // Build method - creates roundhouse structure and track connections
  public RoundhouseInstanceBuilder Build()
  {
    // Only build if we have a turntable reference
    if (Turntable == null)
    {
      logger.Warning("Roundhouse {Identifier} has no turntable reference, skipping build", Id);
      return this;
    }

    // Skip rebuild if stalls haven't changed
    if (Stalls == _lastStalls)
    {
      logger.Debug("Roundhouse {Identifier} stalls unchanged, skipping rebuild", Id);
      return this;
    }

    _lastStalls = Stalls;
    logger.Information("Building roundhouse {Identifier} with {Stalls} stalls, subdivisions={Subdivisions}, templates: stall={Stall}, side={Side}, between={Between}",
      Id, Stalls, Subdivisions, StallTemplate?.name ?? "null", SideTemplate?.name ?? "null", BetweenTemplate?.name ?? "null");

    // Create roundhouse tracks (nodes and segments)
    CreateRoundhouseTracks();

    // Find or create roundhouse container
    var rhContainer = Roundhouse.transform.Find("Roundhouse")?.gameObject ?? new GameObject("Roundhouse");
    rhContainer.transform.DestroyAllChildren(); // Reset
    rhContainer.transform.SetParent(Roundhouse.transform);
    rhContainer.transform.localPosition = new Vector3(0, -0.48f, 0);
    rhContainer.transform.localRotation = Quaternion.identity;
    rhContainer.transform.localScale = Vector3.one;

    // Add KeyValue components
    var rhkv = rhContainer.GetComponent<KeyValueObject>() ?? rhContainer.AddComponent<KeyValueObject>();
    var rhgkv = rhContainer.GetComponent<GlobalKeyValueObject>() ?? rhContainer.AddComponent<GlobalKeyValueObject>();
    rhgkv.globalObjectId = Id + ".roundhouse";

    var interval = 360f / Subdivisions;

    // Build roundhouse by assembling individual pieces
    if (Stalls < Subdivisions)
    {
      // Start wall (position 0)
      BuildRoundhouseSection(rhContainer, 0, interval * 1, true, false);

      // Middle stalls (positions 1 to Stalls-1)
      for (var i = 1; i < Stalls - 1; i++)
      {
        var angle = (i + 1) * interval;
        BuildRoundhouseSection(rhContainer, i, angle, false, false);
      }

      // End wall (position Stalls-1)
      BuildRoundhouseSection(rhContainer, Stalls - 1, interval * Stalls, false, true);
    }
    else
    {
      // Full roundhouse (all positions are middle stalls)
      for (var i = 0; i < Stalls; i++)
      {
        var angle = (i + 1) * interval;
        BuildRoundhouseSection(rhContainer, i, angle, false, false);
      }
    }

    return this;
  }

  // Helper method to build a single roundhouse section from individual pieces
  private void BuildRoundhouseSection(GameObject container, int stallIndex, float angle, bool isStart, bool isEnd)
  {
    if (StallTemplate == null)
    {
      logger.Warning("Stall template is null, skipping section {Index}", stallIndex);
      return;
    }

    // Always instantiate the stall piece
    var stall = UnityEngine.Object.Instantiate(StallTemplate, container.transform);
    stall.transform.localPosition = Vector3.zero;
    stall.transform.localEulerAngles = angle * Vector3.up;
    EnableRenderers(stall);
    PatchDoors(stall, $"stall-doors.{stallIndex}");
    stall.SetActive(true);

    // Add side wall for start/end positions
    if ((isStart || isEnd) && SideTemplate != null)
    {
      var side = UnityEngine.Object.Instantiate(SideTemplate, container.transform);
      side.transform.localPosition = Vector3.zero;
      side.transform.localEulerAngles = new Vector3(0, angle + 180, 0);
      // Start wall is flipped, end wall is not
      side.transform.localScale = isStart ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
      EnableRenderers(side);
      side.SetActive(true);
    }

    // Add between piece (after start, before middle stalls, NOT on end)
    if (!isEnd && BetweenTemplate != null)
    {
      var between = UnityEngine.Object.Instantiate(BetweenTemplate, container.transform);
      between.transform.localPosition = Vector3.zero;
      // Between piece orientation depends on position
      var betweenOffset = isStart ? 11.25f / 2 : -11.25f / 2;
      between.transform.localEulerAngles = new Vector3(270, angle + 180 + betweenOffset, 0);
      EnableRenderers(between);
      between.SetActive(true);
    }
  }

  // Helper method to create roundhouse track nodes and segments
  private void CreateRoundhouseTracks()
  {
    if (Turntable == null || Turntable.turntable == null)
    {
      logger.Warning("Cannot create roundhouse tracks without turntable reference");
      return;
    }

    var interval = 360f / Subdivisions;
    var radius = Turntable.radius;
    var dist = TrackLength + radius;
    var turntablePos = Turntable.transform.position;
    var turntableRotY = Turntable.transform.eulerAngles.y;

    for (var i = 1; i <= Stalls; i++)
    {
      var num = interval * i;
      var quaternion = Quaternion.Euler(0f, turntableRotY + num, 0f);

      // Create roundhouse node at end of stall track
      var nodeId = $"N{Turntable.identifier}RoundhouseNode{i}";
      var nodePos = turntablePos + (quaternion * Vector3.forward * dist);
      var nodeRot = new Vector3(0, turntableRotY + num, 0);

      Helper.Graph.TrackNode(nodeId)
        .At(nodePos)
        .WithRotation(nodeRot);

      // Create segment connecting turntable node to roundhouse node
      var segmentId = $"S{Turntable.identifier}RoundhouseSegment{i}";
      var turntableNodeId = $"N{Turntable.identifier}TurntableNode{i}";

      var startNode = Helper.Graph.TrackNode(turntableNodeId).Node;
      var endNode = Helper.Graph.TrackNode(nodeId).Node;

      Helper.Graph.GetOrCreateTrackSegment(segmentId, startNode, endNode)
        .WithStyle(Track.TrackSegment.Style.Yard);
    }
  }

  // Helper method to enable all renderers on a GameObject
  private void EnableRenderers(GameObject go)
  {
    foreach (var renderer in go.GetComponentsInChildren<Renderer>())
    {
      renderer.enabled = true;
    }
  }

  // Helper method to patch door KeyValue components
  private void PatchDoors(GameObject go, string key)
  {
    var kvt = go.GetComponentInChildren<KeyValuePickableToggle>();
    var kva = go.GetComponentInChildren<KeyValueBoolAnimator>();

    if (kvt == null || kva == null)
    {
      logger.Warning("Missing KeyValuePickableToggle or KeyValueBoolAnimator on {GameObject}", go.name);
      return;
    }

    kvt.key = key;
    kva.key = key;
  }

  // Fluent methods
  public RoundhouseInstanceBuilder WithSubdivisions(int subdivisions)
  {
    Subdivisions = subdivisions;
    return this;
  }

  public RoundhouseInstanceBuilder WithStalls(int stalls)
  {
    Stalls = stalls;
    return this;
  }

  public RoundhouseInstanceBuilder WithTrackLength(int trackLength)
  {
    TrackLength = trackLength;
    return this;
  }

  public RoundhouseInstanceBuilder WithStallTemplate(GameObject template)
  {
    StallTemplate = template;
    return this;
  }

  public RoundhouseInstanceBuilder WithSideTemplate(GameObject template)
  {
    SideTemplate = template;
    return this;
  }

  public RoundhouseInstanceBuilder WithBetweenTemplate(GameObject template)
  {
    BetweenTemplate = template;
    return this;
  }

  public RoundhouseInstanceBuilder WithTurntable(TurntableInstance turntable)
  {
    Turntable = turntable;
    return this;
  }

  public RoundhouseInstanceBuilder At(Vector3 position)
  {
    Position = position;
    return this;
  }

  public RoundhouseInstanceBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));

  public RoundhouseInstanceBuilder WithRotation(Vector3 rotation)
  {
    Euler = rotation;
    return this;
  }

  public RoundhouseInstanceBuilder WithRotation(float x, float y, float z) => WithRotation(new Vector3(x, y, z));

  public RoundhouseInstanceBuilder WithRotation(Quaternion rotation)
  {
    Rotation = rotation;
    return this;
  }

  public RoundhouseInstanceBuilder WithRotation(float y) => WithRotation(Euler.x, y, Euler.z);

  public override RoundhouseInstanceBuilder SetActive(bool active = true)
  {
    Roundhouse.gameObject.SetActive(active);
    return this;
  }
}
