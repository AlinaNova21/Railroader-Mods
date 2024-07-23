using MapEditor.Extensions;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class TrackNodeGhost(string id)
  {

    internal Vector3 _Position;
    internal Vector3 _Rotation;
    internal bool _FlipSwitchStand;

    public TrackNodeGhost(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand)
      : this(id)
    {
      _Position = position;
      _Rotation = rotation;
      _FlipSwitchStand = flipSwitchStand;
    }

    public void UpdateGhost(TrackNode node)
    {
      _Position = node.transform.localPosition;
      _Rotation = node.transform.localEulerAngles;
      _FlipSwitchStand = node.flipSwitchStand;
    }

    public void UpdateNode(TrackNode node)
    {
      node.transform.localPosition = _Position;
      node.transform.localEulerAngles = _Rotation;
      node.flipSwitchStand = _FlipSwitchStand;
    }

    public void CreateNode()
    {
      var gameObject = new GameObject($"Node {id}");
      gameObject.SetActive(false);
      var node = gameObject.AddComponent<TrackNode>();
      node.id = id;
      node.transform.SetParent(Graph.Shared.transform);
      UpdateNode(node);
      gameObject.SetActive(true);
      Graph.Shared.AddNode(node);
      EditorContext.PatchEditor!.AddOrUpdateNode(node);
      EditorContext.AttachUiHelper(node);
    }

    public void DestroyNode()
    {
      var node = Graph.Shared.GetNode(id)!;
      UpdateGhost(node);
      Object.Destroy(node.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.PatchEditor!.RemoveNode(id);
    }

  }
}
