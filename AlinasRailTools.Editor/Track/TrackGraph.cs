using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace AlinasRailTools.Editor.Track;

/// <summary>
/// Track graph with spatial indexing for fast queries
/// </summary>
public class TrackGraph
{
    private Dictionary<string, TrackNodeData> _nodes = new();
    private Dictionary<string, TrackSegmentData> _segments = new();
    private QuadTree _spatialIndex;

    public TrackGraph()
    {
        // Initialize spatial index covering a large area (-10000 to +10000 in XZ)
        _spatialIndex = new QuadTree(new Rectangle(-10000, -10000, 20000, 20000), 10);
    }

    /// <summary>
    /// Load track data from StateFile
    /// </summary>
    public void LoadFromState(StateFile state)
    {
        _nodes.Clear();
        _segments.Clear();
        _spatialIndex.Clear();

        // Load nodes
        var nodesData = state.GetResources("trackNodes");
        foreach (var kvp in nodesData)
        {
            var serialized = kvp.Value.ToObject<SerializedTrackNode>();
            if (serialized == null) continue;

            var node = new TrackNodeData
            {
                Id = kvp.Key,
                Position = new Vector3(
                    serialized.Position.X,
                    serialized.Position.Y,
                    serialized.Position.Z
                ),
                Rotation = new Vector3(
                    serialized.Rotation.X,
                    serialized.Rotation.Y,
                    serialized.Rotation.Z
                ),
                FlipSwitchStand = serialized.FlipSwitchStand
            };

            _nodes[kvp.Key] = node;
            _spatialIndex.Insert(node.Id, node.PositionXZ);
        }

        // Load segments
        var segmentsData = state.GetResources("trackSegments");
        foreach (var kvp in segmentsData)
        {
            var serialized = kvp.Value.ToObject<SerializedTrackSegment>();
            if (serialized == null) continue;

            var segment = new TrackSegmentData
            {
                Id = kvp.Key,
                StartId = serialized.StartId,
                EndId = serialized.EndId,
                Priority = serialized.Priority,
                GroupId = serialized.GroupId,
                Style = serialized.Style
            };

            // Generate curve points if both nodes exist
            if (_nodes.TryGetValue(segment.StartId, out var startNode) &&
                _nodes.TryGetValue(segment.EndId, out var endNode))
            {
                segment.GenerateCurvePoints(startNode, endNode);

                // Update node connections
                startNode.ConnectedSegmentIds.Add(segment.Id);
                endNode.ConnectedSegmentIds.Add(segment.Id);
            }

            _segments[kvp.Key] = segment;
        }

        Console.WriteLine($"Loaded {_nodes.Count} nodes and {_segments.Count} segments");
    }

    /// <summary>
    /// Get all nodes
    /// </summary>
    public IEnumerable<TrackNodeData> GetAllNodes() => _nodes.Values;

    /// <summary>
    /// Get all segments
    /// </summary>
    public IEnumerable<TrackSegmentData> GetAllSegments() => _segments.Values;

    /// <summary>
    /// Get nodes within radius of a point (XZ plane)
    /// </summary>
    public IEnumerable<TrackNodeData> GetNodesInRadius(Vector3 center, float radius)
    {
        var nearbyIds = _spatialIndex.QueryRadius(new Vector2(center.X, center.Z), radius);
        return nearbyIds.Select(id => _nodes[id]);
    }

    /// <summary>
    /// Get node by ID
    /// </summary>
    public TrackNodeData? GetNode(string id)
    {
        return _nodes.TryGetValue(id, out var node) ? node : null;
    }

    /// <summary>
    /// Get segment by ID
    /// </summary>
    public TrackSegmentData? GetSegment(string id)
    {
        return _segments.TryGetValue(id, out var segment) ? segment : null;
    }
}

/// <summary>
/// Simple 2D rectangle for quad tree
/// </summary>
public struct Rectangle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= X && point.X <= X + Width &&
               point.Y >= Y && point.Y <= Y + Height;
    }

    public bool Intersects(Rectangle other)
    {
        return X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }
}

/// <summary>
/// Simple QuadTree for spatial indexing (XZ plane)
/// </summary>
public class QuadTree
{
    private Rectangle _bounds;
    private int _capacity;
    private List<(string id, Vector2 pos)> _points = new();
    private QuadTree[]? _children;
    private bool _divided;

    public QuadTree(Rectangle bounds, int capacity = 10)
    {
        _bounds = bounds;
        _capacity = capacity;
    }

    public void Clear()
    {
        _points.Clear();
        _children = null;
        _divided = false;
    }

    public void Insert(string id, Vector2 position)
    {
        if (!_bounds.Contains(position))
            return;

        if (_points.Count < _capacity)
        {
            _points.Add((id, position));
            return;
        }

        if (!_divided)
        {
            Subdivide();
        }

        foreach (var child in _children!)
        {
            child.Insert(id, position);
        }
    }

    public List<string> QueryRadius(Vector2 center, float radius)
    {
        var result = new List<string>();
        QueryRadiusInternal(center, radius, result);
        return result;
    }

    private void QueryRadiusInternal(Vector2 center, float radius, List<string> result)
    {
        // Check if circle intersects this quad
        float radiusSq = radius * radius;

        foreach (var (id, pos) in _points)
        {
            float distSq = Vector2.DistanceSquared(center, pos);
            if (distSq <= radiusSq)
            {
                result.Add(id);
            }
        }

        if (_divided)
        {
            foreach (var child in _children!)
            {
                child.QueryRadiusInternal(center, radius, result);
            }
        }
    }

    private void Subdivide()
    {
        float halfWidth = _bounds.Width / 2;
        float halfHeight = _bounds.Height / 2;

        _children = new QuadTree[4];
        _children[0] = new QuadTree(new Rectangle(_bounds.X, _bounds.Y, halfWidth, halfHeight), _capacity);
        _children[1] = new QuadTree(new Rectangle(_bounds.X + halfWidth, _bounds.Y, halfWidth, halfHeight), _capacity);
        _children[2] = new QuadTree(new Rectangle(_bounds.X, _bounds.Y + halfHeight, halfWidth, halfHeight), _capacity);
        _children[3] = new QuadTree(new Rectangle(_bounds.X + halfWidth, _bounds.Y + halfHeight, halfWidth, halfHeight), _capacity);

        _divided = true;

        // Redistribute existing points
        foreach (var (id, pos) in _points)
        {
            foreach (var child in _children)
            {
                child.Insert(id, pos);
            }
        }
    }
}
