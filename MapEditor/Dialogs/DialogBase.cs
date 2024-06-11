using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs
{
  public abstract class DialogBase
  {

    private bool _Populated;
    private readonly Window _Window;

    public DialogBase(string title, int width, int height, Window.Position position)
    {
      _Window = EditorContext.UIHelper.CreateWindow(width, height, position);
      _Window.Title = title;

      _Window.OnShownDidChange += shown =>
      {
        if (shown)
        {
          AfterWindowOpen();
        }
        else
        {
          AfterWindowClosed();
        }
      };
    }

    protected abstract void BuildWindow(UIPanelBuilder builder);

    public void ShowWindow(string title)
    {
      _Window.Title = title;
      ShowWindow();
    }

    public void ShowWindow()
    {
      if (!_Populated)
      {
        EditorContext.UIHelper.PopulateWindow(_Window, BuildWindow);
        _Populated = true;
      }

      BeforeWindowShown();

      if (!_Window.IsShown)
      {
        _Window.ShowWindow();
      }
    }

    public void CloseWindow()
    {
      _Window.CloseWindow();
    }

    protected virtual void BeforeWindowShown()
    {
    }

    protected virtual void AfterWindowOpen()
    {
    }

    protected virtual void AfterWindowClosed()
    {
    }

    public string Title
    {
      get => _Window.Title;
      set => _Window.Title = value;
    }

  }
}
