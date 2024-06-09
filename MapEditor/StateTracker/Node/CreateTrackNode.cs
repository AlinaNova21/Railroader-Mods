namespace MapEditor.StateTracker.Node {
  using JetBrains.Annotations;
  using UnityEngine;

  public sealed class CreateTrackNode : IUndoable {

    private readonly string _id;
    private readonly TrackNodeGhost _ghost;

    public CreateTrackNode(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand = false) {
      _id = id;
      _ghost = new TrackNodeGhost(id, position, rotation, flipSwitchStand);
    }

    public void Apply() {
      _ghost.CreateNode();
    }

    public void Revert() {
      _ghost.DestroyNode();
    }

    public override string ToString() {
      return "CreateTrackNode: " + _id;
    }

  }
}
