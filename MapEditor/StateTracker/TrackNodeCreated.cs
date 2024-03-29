using System.Runtime.Remoting.Contexts;
using Helpers;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
    public class TrackNodeCreated : IUndoable
    {
        private TrackNode _node;
        private string _id;
        private Vector3 _position;
        private Vector3 _rotation;

        public TrackNodeCreated(string id, Vector3 position, Vector3 rotation)
        {
            _id = id;
            _position = position;
            _rotation = rotation;
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
          EditorContext.Instance.PatchEditor.AddOrUpdateNode(_node.id, _position, _rotation, false);
        }

        public void Revert()
        {
          UnityEngine.Object.Destroy(_node.gameObject);
          Graph.Shared.RebuildCollections();
          EditorContext.Instance.PatchEditor.RemoveNode(_id);
        }
    }
}