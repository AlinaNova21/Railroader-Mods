namespace MapEditor.StateTracker.Node {
  using JetBrains.Annotations;
  using MapEditor.Extensions;
  using Track;
  using UnityEngine;

  public sealed class TrackNodeGhost {

    private readonly string _id;

    private Vector3 _position;
    private Vector3 _rotation;
    private bool _flipSwitchStand;

    public TrackNodeGhost(string id) {
      _id = id;
    }

    public TrackNodeGhost(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand)
      : this(id) {
      _position = position;
      _rotation = rotation;
      _flipSwitchStand = flipSwitchStand;
    }

    public void UpdateGhost(TrackNode node) {
      _position = node.transform.localPosition;
      _rotation = node.transform.localEulerAngles;
      _flipSwitchStand = node.flipSwitchStand;
    }

    public void UpdateNode(TrackNode node) {
      node.transform.localPosition = _position;
      node.transform.localEulerAngles = _rotation;
      node.flipSwitchStand = _flipSwitchStand;
    }

    public void CreateNode() {
      var node = new GameObject($"Node {_id}").AddComponent<TrackNode>();
      node.id = _id;
      node.transform.SetParent(Graph.Shared.transform);
      UpdateNode(node);
      Graph.Shared.AddNode(node);
      EditorContext.Instance.PatchEditor.AddOrUpdateNode(node);
    }

    public void DestroyNode() {
      var node = Graph.Shared.GetNode(_id);
      UpdateGhost(node);
      Object.Destroy(node.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.Instance.PatchEditor.RemoveNode(_id);
    }

  }
}
