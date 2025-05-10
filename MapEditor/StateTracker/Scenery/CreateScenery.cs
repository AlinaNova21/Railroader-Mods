using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateScenery : IUndoable
  {

    private readonly string _id;
    private readonly SceneryGhost _ghost;

    public CreateScenery(string id, Vector3 position, Vector3 rotation, Vector3 scale, string model)
    {
      _id = id;
      _ghost = new SceneryGhost(id, position, rotation, scale, model);
    }

    public void Apply()
    {
      _ghost.CreateScenery();
    }

    public void Revert()
    {
      _ghost.DestroyScenery();
    }

    public override string ToString()
    {
      return "CreateScenery: " + _id;
    }

  }
}
