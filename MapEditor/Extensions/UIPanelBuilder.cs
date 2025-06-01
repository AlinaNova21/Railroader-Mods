using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor.Extensions
{
  [PublicAPI]
  public static class UIPanelBuilderExtensions
  {

    public static RectTransform AddIconButton(this UIPanelBuilder builder, Sprite icon, Action action)
    {
      var container = (RectTransform)AccessTools.Field(typeof(UIPanelBuilder), "_container")!.GetValue(builder)!;
      var button = new GameObject("Button").AddComponent<Button>();
      var rect = button.gameObject.AddComponent<RectTransform>();
      button.transform.SetParent(container, false);
      button.onClick.AddListener(action.Invoke);

      var img = button.gameObject.AddComponent<Image>();
      img.sprite = icon;
      img.rectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48);
      img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
      rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48);
      rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
      rect.Width(48);
      rect.Height(48);
      return rect;
    }

    public static RectTransform AddPopupMenu(this UIPanelBuilder builder, string title, params PopupMenuItem[] items)
    {
      List<string> values = [title, .. items.Select(o => o.DisplayName)];

      TMP_Dropdown dropdown = null!;
      var rect = builder.AddDropdown(values, 0, o => {
        if (o == 0) {
          return;
        }

        items[o - 1].OnSelected();
        // ReSharper disable once AccessToModifiedClosure
        dropdown.value = 0;
      })!;

      rect.FlexibleWidth();
      dropdown = rect.GetComponent<TMP_Dropdown>()!;
      dropdown.MultiSelect = false;
      return rect;
    }

    #region AddEnumDropdown

    private static readonly Dictionary<Type, List<string>> _EnumValues = new Dictionary<Type, List<string>>();

    private static List<string> GetEnumValues<T>()
    {
      var type = typeof(T);
      if (!_EnumValues.TryGetValue(type, out var values)) {
        values = new List<string>();
        foreach (var value in Enum.GetValues(type)) {
          values.Add(value.ToString());
        }

        _EnumValues.Add(type, values);
      }

      return values!;
    }

    public static RectTransform AddEnumDropdown<T>(this UIPanelBuilder builder, T current, Action<T> onSelected) where T : Enum
    {
      var values = GetEnumValues<T>();
      var currentSelectedIndex = values.IndexOf(current.ToString());
      var rect = builder.AddDropdown(values, currentSelectedIndex, o => onSelected((T)Enum.Parse(typeof(T), values[o]!)))!;
      var tmpDropdown = rect.GetComponent<TMP_Dropdown>()!;
      tmpDropdown.MultiSelect = false;
      return rect;
    }

    #endregion

  }

  public class PopupMenuItem(string displayName, Action onSelected)
  {

    public string DisplayName { get; } = displayName;
    public Action OnSelected { get; } = onSelected;

  }
}
