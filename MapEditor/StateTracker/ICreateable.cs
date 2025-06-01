namespace MapEditor.StateTracker
{
  public interface ICreateable
  {
    void Create();
    void Destroy();
    string GetId();
  }
}
