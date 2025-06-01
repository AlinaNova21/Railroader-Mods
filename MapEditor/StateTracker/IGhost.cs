namespace MapEditor.StateTracker
{
  public interface IGhost
  {
    string GetId();
    void SetId(string id);
    void SetProperty<T>(string property, T value);
    T GetProperty<T>(string property);
  }
}
