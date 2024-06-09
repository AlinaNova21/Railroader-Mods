using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Serilog;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor.Extensions
{
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
      button.onClick.AddListener(action.Invoke);
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

    public static RectTransform AddPopupMenu(this UIPanelBuilder builder, params PopupMenuItem[] items)
    {
      List<string> values = ["More ...", .. items.Select(o => o.DisplayName)];

      TMP_Dropdown dd = null!;
      var rect = builder.AddDropdown(values, 0, o =>
      {
        if (o == 0)
        {
          return;
        }

        items[o - 1].OnSelected();
        dd.value = 0;
      });
      rect.FlexibleWidth();
      dd = rect.GetComponent<TMP_Dropdown>();
      dd.MultiSelect = false;
      return rect;
    }

  }

  public class PopupMenuItem
  {

    public PopupMenuItem(string displayName, Action onSelected)
    {
      DisplayName = displayName;
      OnSelected = onSelected;
    }

    public string DisplayName { get; set; }
    public Action OnSelected { get; set; }

  }
}
