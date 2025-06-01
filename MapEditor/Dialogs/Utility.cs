using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Definitions;
using MapEditor.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Dialogs;

public static class Clipboard
{
  public static string Raw
  {
    get => GUIUtility.systemCopyBuffer;
    set => GUIUtility.systemCopyBuffer = value;
  }

  public static void Set<T>(T value)
  {
    if (value == null) return;
    if (typeof(T) == typeof(string)) {
      GUIUtility.systemCopyBuffer = value as string;
      return;
    }
    var json = JToken.FromObject(value);
    GUIUtility.systemCopyBuffer = json.ToString();
  }

  public static T? Get<T>()
  {
    if (typeof(T) == typeof(string)) {
      var str = GUIUtility.systemCopyBuffer;
      return (T)(object)(str ?? "");
    }
    var json = GUIUtility.systemCopyBuffer;
    if (string.IsNullOrEmpty(json))
      return default!;
    try {
      var value = JsonConvert.DeserializeObject<T>(json);
      return value ?? default;
    } catch (JsonException) {
      return default!;
    }
  }

  public static Vector3 Vector3
  {
    get {
      var json = GUIUtility.systemCopyBuffer;
      if (string.IsNullOrEmpty(json))
        return Vector3.zero;
      try {
        var vector = JsonConvert.DeserializeObject<SerializedVector3>(json);
        return vector ?? Vector3.zero;
      } catch (JsonException) {
        return Vector3.zero;
      }
    }
    set {
      var json = JObject.FromObject((SerializedVector3)value);
      GUIUtility.systemCopyBuffer = json.ToString();
    }
  }
}
public static class Utility
{
  public static JsonSerializer JsonSerializer { get; } = new JsonSerializer
  {
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
    TypeNameHandling = TypeNameHandling.Auto,
  };

  public static void BuildScalingEditor(UIPanelBuilder builder)
  {
    var sizeScaling = new List<ValueTuple<string, int>>() {
      ("0.01", 1),
      ("0.1", 10),
      ("0.5", 50),
      ("1", 100),
      ("5", 500),
      ("10", 1000),
      ("25", 2500),
      ("50", 5000),
      ("100", 10000),
      ("1000", 100000)
    };
    var rotationScaling = new List<ValueTuple<string, int>>() {
      ("0.01", 1),
      ("0.1", 10),
      ("0.5", 50),
      ("1", 100),
      ("5", 500),
      ("10", 1000),
      ("15", 1500),
      ("30", 3000),
      ("45", 4500),
      ("60", 6000),
      ("90", 9000),
      ("180", 18000)
    };

    var doDropdown = (List<ValueTuple<string, int>> options, int selected, Action<int> act) => {
      var selectedIndex = options.FindIndex(o => o.Item2 == selected);
      return builder.AddDropdown(options.Select(o => o.Item1).ToList(), selectedIndex, (index) => act(options[index].Item2));
    };
    builder.Spacer(20);
    builder.AddSection("Movement", (builder) => {
      builder.HStack(stack => {
        for (int i = 0; i < sizeScaling.Count; i++) {
          var value = sizeScaling[i].Item2;
          var label = sizeScaling[i].Item1;
          stack.AddButtonSelectable(label, EditorContext.Scaling.Movement == value, () => {
            EditorContext.Scaling.Movement = value;
            builder.Rebuild();
          });
        }
      });
    });
    builder.Spacer(20);
    builder.AddSection("Rotation", (builder) => {
      builder.HStack(stack => {
        for (int i = 0; i < rotationScaling.Count; i++) {
          var value = rotationScaling[i].Item2;
          var label = rotationScaling[i].Item1;
          stack.AddButtonSelectable(label, EditorContext.Scaling.Rotation == value, () => {
            EditorContext.Scaling.Rotation = value;
            builder.Rebuild();
          });
        }
      });
    });

    //builder.AddField("Rotation", doDropdown(
    //  rotationScaling,
    //  EditorContext.Scaling.Rotation,
    //  (value) => EditorContext.Scaling.Rotation = value
    //));
    //builder.AddField("Scale", doDropdown(
    //  sizeScaling,
    //  sizeScalingValue,
    //  EditorContext.Scaling.Scale,
    //  (value) => EditorContext.Scaling.Scale = value
    //));
  }
  public static void BuildPositionEditor(UIPanelBuilder builder, Action<Direction> action)
  {
    action ??= dir => { };
    var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
    builder.HStack(stack => {
      stack.AddButtonCompact(() => $"- {EditorContext.Scaling.Movement / 100f:F2}", () => action(Direction.down));
      stack.AddIconButton(arrowUp, () => action(Direction.forward));
      stack.AddButtonCompact(() => $"+ {EditorContext.Scaling.Movement / 100f:F2}", () => action(Direction.up));
    });
    builder.HStack(stack => {
      var left = stack.AddIconButton(arrowUp, () => action(Direction.left));
      left.localEulerAngles += new Vector3(0, 0, 90);
      var down = stack.AddIconButton(arrowUp, () => action(Direction.backward));
      down.localEulerAngles += new Vector3(0, 0, 180);
      var right = stack.AddIconButton(arrowUp, () => action(Direction.right));
      right.localEulerAngles += new Vector3(0, 0, -90);
    });
  }

  public static void BuildRotationEditor(UIPanelBuilder builder, Action<Vector3> action)
  {
    action ??= dir => { };
    var xPos = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 256, 256, -256), new Vector2(0.5f, 0.5f))!;
    var xNeg = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
    var yPos = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
    var yNeg = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
    var zPos = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
    var zNeg = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
    builder.HStack(stack => {
      stack.AddIconButton(zNeg, () => action(Vector3.back));
      stack.AddIconButton(xPos, () => action(Vector3.right));
      stack.AddIconButton(zPos, () => action(Vector3.forward));
    });
    builder.HStack(stack => {
      stack.AddIconButton(yNeg, () => action(Vector3.down));
      stack.AddIconButton(xNeg, () => action(Vector3.left));
      stack.AddIconButton(yPos, () => action(Vector3.up));
    });
  }
}
