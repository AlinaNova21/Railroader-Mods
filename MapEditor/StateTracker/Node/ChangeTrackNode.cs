namespace MapEditor.StateTracker.Node {
  using JetBrains.Annotations;
  using MapEditor.Extensions;
  using Track;
  using UnityEngine;

  public sealed class ChangeTrackNode : IUndoable {

    private readonly TrackNode _node;
    private readonly TrackNodeGhost _old;
    private readonly TrackNodeGhost _new;

    public ChangeTrackNode(TrackNode node, Vector3 newPosition, Vector3 newRotation, bool flipSwitchStand) {
      _node = node;
      _old = new TrackNodeGhost(node.id!);
      _new = new TrackNodeGhost(node.id, newPosition, newRotation, flipSwitchStand);
    }

    public void Apply() {
      _new.UpdateNode(_node);
      EditorContext.Instance.PatchEditor.AddOrUpdateNode(_node);
      Graph.Shared.OnNodeDidChange(_node);
    }

    public void Revert() {
      _old.UpdateNode(_node);
      EditorContext.Instance.PatchEditor.AddOrUpdateNode(_node);
      Graph.Shared.OnNodeDidChange(_node);
    }

  }
}
