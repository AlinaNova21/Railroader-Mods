using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasMapMod.Windows;

public abstract class WindowBase : MonoBehaviour, IProgrammaticWindow
{
  protected Window Window => GetComponent<Window>();
  public UIBuilderAssets BuilderAssets { get; set; }
  public abstract string WindowIdentifier { get; }
  public abstract string Title { get; }
  public abstract Vector2Int DefaultSize { get; }
  public abstract Window.Position DefaultPosition { get; }
  public abstract Window.Sizing Sizing { get; }
  public abstract void Populate(UIPanelBuilder builder);

  private UIPanel _panel;

  public void Rebuild()
  {
    if (_panel != null)         _panel.Dispose();
    Window.Title = Title;
    _panel = UIPanel.Create(Window.contentRectTransform, BuilderAssets, new System.Action<UIPanelBuilder>(Populate));
  }
}
