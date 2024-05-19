using System;
using HarmonyLib;
using Serilog;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor.Extensions;
public static class UIPanelBuilderExtensions
{
  public static RectTransform AddIconButton(this UIPanelBuilder builder, Sprite icon, Action action)
  {
    var builderAssets = AccessTools.Field(typeof(UIPanelBuilder), "_assets").GetValue(builder) as UIBuilderAssets;
    var container = AccessTools.Field(typeof(UIPanelBuilder), "_container").GetValue(builder) as RectTransform;
    // var button = UnityEngine.Object.Instantiate(builderAssets.button, container, false);
    var button = new GameObject("Button").AddComponent<Button>();
    var rect = button.gameObject.AddComponent<RectTransform>();
    button.transform.SetParent(container, false);
    button.onClick.AddListener(new UnityEngine.Events.UnityAction(action.Invoke));
    foreach (var c in button.GetComponents<Component>())
    {
      Log.Debug("Component: {0}", c.GetType().Name);
    }
    var img = button.gameObject.AddComponent<Image>();
    img.sprite = icon;
    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48);
    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48);
    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
    rect.Width(48);
    rect.Height(48);
    return rect;
  }
}
