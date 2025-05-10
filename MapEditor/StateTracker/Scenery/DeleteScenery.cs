
using Helpers;

namespace MapEditor.StateTracker.Node
{
  public sealed class DeleteScenery(SceneryAssetInstance scenery) : IUndoable
  {

    private readonly string _Id = scenery.name;
    private SceneryGhost? _Ghost;

    public void Apply()
    {
      _Ghost = new SceneryGhost(_Id);
      _Ghost.DestroyScenery();
    }

    public void Revert()
    {
      _Ghost!.CreateScenery();
    }

    public override string ToString()
    {
      return "DeleteScenery: " + _Id;
    }

  }
}
