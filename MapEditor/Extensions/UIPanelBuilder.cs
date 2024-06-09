using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Serilog;
using TMPro;
using Track;
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

    private static readonly List<string> _trackStyles = Enum.GetValues(typeof(TrackSegment.Style)).Cast<TrackSegment.Style>().Select(o => o.ToString()).ToList();

    public static RectTransform AddTrackStylesDropdown(this UIPanelBuilder builder, TrackSegment.Style current, Action<TrackSegment.Style> onSelected)
    {
      return builder.AddDropdown(_trackStyles, _trackStyles.IndexOf(current.ToString()), o => onSelected((TrackSegment.Style)o))!;
    }

    private static readonly List<string> _trackClasses = Enum.GetValues(typeof(TrackClass)).Cast<TrackClass>().Select(o => o.ToString()).ToList();

    public static RectTransform AddTrackClassDropdown(this UIPanelBuilder builder, TrackClass current, Action<TrackClass> onSelected)
    {
      return builder.AddDropdown(_trackClasses, _trackClasses.IndexOf(current.ToString()), o => onSelected((TrackClass)o))!;
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
