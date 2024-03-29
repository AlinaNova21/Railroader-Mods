namespace MapEditor.Tools
{
  public abstract class BaseTool
  {
    protected abstract string ToolIconPath { get; }
    protected abstract string ToolName { get; }
    protected abstract string ToolDescription { get; }

    protected EditorContext Context { get; private set; }

    public void Activate() {
      Context = EditorContext.Instance;
      Context.SetActiveTool(this);
      EditorContext.EditorContextChanged += OnEditorContextChanged;
      OnActivated();
    }

    public void Deactivate()
    {
      OnDeactivating();
      EditorContext.EditorContextChanged -= OnEditorContextChanged;
      Context.SetActiveTool(null);
    }

    protected abstract void OnDeactivating();
    public abstract void OnActivated();

    protected virtual void OnEditorContextChanged(EditorContext context)
    {
    }
  }
}