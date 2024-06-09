using System;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as CreateTrackNode")]
  public class TrackNodeCreated : IUndoable
  {

    private TrackNode _node;
    private readonly string _id;
    private readonly Vector3 _position;
    private readonly Vector3 _rotation;
    private bool _flipSwitchStand;

    public TrackNodeCreated(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand = false)
    {
      _id = id;
      _position = position;
      _rotation = rotation;
      _flipSwitchStand = flipSwitchStand;
    }

    public void Apply()
    {
      var newNode = new GameObject($"Node {_id}").AddComponent<TrackNode>();
      newNode.id = _id;
      newNode.transform.SetParent(Graph.Shared.transform);
      newNode.transform.localPosition = _position;
      newNode.transform.localEulerAngles = _rotation;
      newNode.flipSwitchStand = false;
      Graph.Shared.AddNode(newNode);
      _node = newNode;
      EditorContext.Instance.PatchEditor.AddOrUpdateNode(_node.id, _position, _rotation);
    }

    public void Revert()
    {
      UnityEngine.Object.Destroy(_node.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.Instance.PatchEditor.RemoveNode(_id);
    }

  }
}
