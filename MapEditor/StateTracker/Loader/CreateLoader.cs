using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateLoader : IUndoable
  {

    private readonly string _id;
    private readonly LoaderGhost _ghost;

    public CreateLoader(string id, Vector3 position, Vector3 rotation, string prefab, string industry)
    {
      _id = id;
      _ghost = new LoaderGhost(id, position, rotation, prefab, industry);
    }

    public void Apply()
    {
      _ghost.CreateLoader();
    }

    public void Revert()
    {
      _ghost.DestroyLoader();
    }

    public override string ToString()
    {
      return "CreateLoader: " + _id;
    }

  }
}
