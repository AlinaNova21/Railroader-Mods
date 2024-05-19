using System.Runtime.Remoting.Contexts;
using Helpers;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
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
