using System;
using System.Collections.Generic;
using Helpers;
using MapEditor.Extensions;
using MapEditor.Managers;
using Model.Ops;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs
{
  public sealed class ObjectDialog() : DialogBase("ObjectEditor", "Node editor", 400, 350, Window.Position.CenterRight)
  {

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Type", () => EditorContext.SelectedObject?.DisplayType ?? "Unknown", UIPanelBuilder.Frequency.Periodic);
      if (EditorContext.SelectedObject is ITransformableObject transformable)
      {
        builder.AddField("Position", () => transformable.Position.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
        builder.AddField("Rotation", () => transformable.Rotation.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
        builder.Spacer();
        builder.HStack(stack =>
        {
          stack.AddSection("Position", BuildPositionEditor);
          stack.Spacer();
          stack.AddSection("Rotation", BuildRotationEditor);
        });
        builder.AddSection("Scaling", Utility.BuildScalingEditor);
      }
      builder.AddSection(EditorContext.SelectedObject?.DisplayType ?? "Object", builder => EditorContext.SelectedObject?.BuildUI(builder));
      builder.AddExpandingVerticalSpacer();
    }


    private static void BuildPositionEditor(UIPanelBuilder builder)
    {
      var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"- {ObjectManager.Scaling:F2}", () => ObjectManager.Move(Direction.down));
        stack.AddIconButton(arrowUp, () => ObjectManager.Move(Direction.forward));
        stack.AddButtonCompact(() => $"+ {ObjectManager.Scaling:F2}", () => ObjectManager.Move(Direction.up));
      });
      builder.HStack(stack =>
      {
        var left = stack.AddIconButton(arrowUp, () => ObjectManager.Move(Direction.left));
        left.localEulerAngles += new Vector3(0, 0, 90);
        var down = stack.AddIconButton(arrowUp, () => ObjectManager.Move(Direction.backward));
        down.localEulerAngles += new Vector3(0, 0, 180);
        var right = stack.AddIconButton(arrowUp, () => ObjectManager.Move(Direction.right));
        right.localEulerAngles += new Vector3(0, 0, -90);
      });
    }

    private static void BuildRotationEditor(UIPanelBuilder builder)
    {
      var xPos = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 256, 256, -256), new Vector2(0.5f, 0.5f))!;
      var xNeg = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var yPos = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var yNeg = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var zPos = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var zNeg = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddIconButton(zNeg, () => ObjectManager.Rotate(Vector3.back));
        stack.AddIconButton(xPos, () => ObjectManager.Rotate(Vector3.right));
        stack.AddIconButton(zPos, () => ObjectManager.Rotate(Vector3.forward));
      });
      builder.HStack(stack =>
      {
        stack.AddIconButton(yNeg, () => ObjectManager.Rotate(Vector3.down));
        stack.AddIconButton(xNeg, () => ObjectManager.Rotate(Vector3.left));
        stack.AddIconButton(yPos, () => ObjectManager.Rotate(Vector3.up));
      });
    }


    protected override void BeforeWindowShown()
    {
      base.BeforeWindowShown();
    }

    protected override void AfterWindowClosed()
    {
      base.AfterWindowClosed();
      EditorContext.SelectedObject = null;
    }

  }
}
