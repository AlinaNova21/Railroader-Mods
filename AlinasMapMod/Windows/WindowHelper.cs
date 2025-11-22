using System;
using UI;
using UI.Common;
using UnityEngine;

namespace AlinasMapMod.Windows;
internal class WindowHelper
{
  private static ProgrammaticWindowCreator _programmaticWindowCreator;
  private static ProgrammaticWindowCreator ProgrammaticWindowCreator
  {
    get {
      if (_programmaticWindowCreator == null)         _programmaticWindowCreator = UnityEngine.Object.FindObjectOfType<ProgrammaticWindowCreator>(true);
      return _programmaticWindowCreator;
    }
  }

  private static Window CreateWindow()
  {
    var createWindow = typeof(ProgrammaticWindowCreator).GetMethod("CreateWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, [], null);
    return (Window)createWindow.Invoke(ProgrammaticWindowCreator, null);
  }

  public static void CreateWindow<TWindow>(Action<TWindow> configure) where TWindow : Component, IProgrammaticWindow
  {
    Window window = CreateWindow();
    window.name = typeof(TWindow).ToString();
    TWindow twindow = window.gameObject.AddComponent<TWindow>();
    twindow.BuilderAssets = ProgrammaticWindowCreator.builderAssets;
    window.CloseWindow();
    window.SetInitialPositionSize(twindow.WindowIdentifier, twindow.DefaultSize, twindow.DefaultPosition, twindow.Sizing);
    if (configure != null)       configure(twindow);

    //Type[] paramTypes = [typeof(Action<TWindow>)];
    //var createWindow = typeof(ProgrammaticWindowCreator).GetMethod("CreateWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, paramTypes, null);
    //createWindow.MakeGenericMethod(typeof(TWindow)).Invoke(ProgrammaticWindowCreator, [configure]);
  }

  public static void CreateWindow<TWindow>(string identifier, int width, int height, Window.Position position, Window.Sizing sizing = default, Action<TWindow> configure = null) where TWindow : Component, IBuilderWindow
  {
    Type[] paramTypes = [typeof(string), typeof(int), typeof(int), typeof(Window.Position), typeof(Window.Sizing), typeof(Action<TWindow>)];
    var createWindow = typeof(ProgrammaticWindowCreator).GetMethod("CreateWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, paramTypes, null);
    createWindow.MakeGenericMethod(typeof(TWindow)).Invoke(ProgrammaticWindowCreator, [identifier, width, height, position, sizing, configure]);
  }
}
