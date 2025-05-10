using Track;
using static AlinasMapMod.LoaderBuilder;

namespace MapEditor.StateTracker.Node
{
  public sealed class DeleteLoader(CustomLoader loader) : IUndoable
  {

    private readonly string _Id = loader.id;
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
}
