using AlinasMapMod.Loaders;

namespace MapEditor.StateTracker.Node;

public sealed class DeleteLoader(LoaderInstance loader) : IUndoable
{

  private readonly string _Id = loader.identifier;
  private LoaderGhost? _Ghost;

  public void Apply()
  {
    _Ghost = new LoaderGhost(_Id);
    _Ghost.DestroyLoader();
  }

  public void Revert()
  {
    _Ghost!.CreateLoader();
  }

  public override string ToString()
  {
    return "DeleteLoader: " + _Id;
  }
}
