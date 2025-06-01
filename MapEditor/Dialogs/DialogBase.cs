using System;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs
{
  public abstract class DialogBase
  {
    private bool _Populated;
    private readonly Window _Window;

    protected DialogBase(string identifier, string title, int width, int height, Window.Position position)
    {
      _Window = EditorContext.UIHelper.CreateWindow(identifier, width, height, position);
      _Window.Title = title;

      _Window.OnShownDidChange += shown => {
        if (!shown) {
          AfterWindowClosed();
        }
      };
    }

    [Obsolete("Use the constructor with identifier parameter instead.")]
    protected DialogBase(string title, int width, int height, Window.Position position)
    {
      _Window = EditorContext.UIHelper.CreateWindow(width, height, position);
      _Window.Title = title;

      _Window.OnShownDidChange += shown => {
        if (!shown) {
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
      if (!_Populated) {
        EditorContext.UIHelper.PopulateWindow(_Window, BuildWindow);
        _Populated = true;
      }

      BeforeWindowShown();

      if (!_Window.IsShown) {
        _Window.ShowWindow();
      }
    }

    public void Rebuild()
    {
      EditorContext.UIHelper.PopulateWindow(_Window, BuildWindow);
    }

    public void CloseWindow()
    {
      _Window.CloseWindow();
    }

    protected virtual void BeforeWindowShown() { }

    protected virtual void AfterWindowClosed() { }
  }
}
