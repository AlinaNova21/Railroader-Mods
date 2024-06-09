using System;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as DeleteTrackNode")]
  public class TrackNodeDeleted : TrackNodeCreated
  {

    public TrackNodeDeleted(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand) : base(id, position, rotation, flipSwitchStand)
    {
    }

    public new void Apply()
    {
      base.Revert();
    }

    public new void Revert()
    {
      base.Apply();
    }

  }
}
