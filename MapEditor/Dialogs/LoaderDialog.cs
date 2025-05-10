using System;
using System.Collections.Generic;
using MapEditor.Extensions;
using MapEditor.Managers;
using Model.Ops;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs
{
  public sealed class LoaderDialog() : DialogBase("loaderEditor", "Node editor", 400, 350, Window.Position.CenterRight)
  {

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Position", () => EditorContext.SelectedLoader?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Rotation", () => EditorContext.SelectedLoader?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.Spacer();
      builder.HStack(stack =>
      {
        stack.AddSection("Position", BuildPositionEditor);
        stack.Spacer();
        stack.AddSection("Rotation", BuildRotationEditor);
      });
      builder.AddSection("Scaling", Utility.BuildScalingEditor);
      builder.AddSection("Loader", BuildLoaderEditor);
      builder.AddExpandingVerticalSpacer();
    }

    private static void BuildLoaderEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack =>
      {
        stack.AddButtonCompact("Remove", () => LoaderManager.RemoveLoader());
        stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedLoader);
      });
      builder.Spacer();
      var prefabs = new List<string>
      {
        "coalConveyor",
        "coalTower",
        "dieselFuelingStand",
        "waterTower",
        "waterColumn"
      };
      var selectedValue = EditorContext.SelectedLoader?.config.Prefab.Replace("vanilla://", "");
      var selectedIndex = prefabs.IndexOf(selectedValue);
      builder.AddDropdown(prefabs, selectedIndex, (index) =>
      {
        var prefab = prefabs[index];
        LoaderManager.Prefab("vanilla://" + prefab);
      });
      var industries = new List<string>();
      var allIndustries = GameObject.FindObjectsOfType<Industry>();
      foreach (var industry in allIndustries)
      {
        industries.Add(industry.identifier);
      }
      var industrySelectedIndex = industries.IndexOf(EditorContext.SelectedLoader!.config.Industry);
      industries.Sort();
      builder.AddDropdown(industries, industrySelectedIndex, (index) =>
      {
        var industry = industries[index];
        LoaderManager.Industry(industry);
      });
    }

    private static void BuildPositionEditor(UIPanelBuilder builder)
    {
      var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"- {LoaderManager.Scaling:F2}", () => LoaderManager.Move(Direction.down));
        stack.AddIconButton(arrowUp, () => LoaderManager.Move(Direction.forward));
        stack.AddButtonCompact(() => $"+ {LoaderManager.Scaling:F2}", () => LoaderManager.Move(Direction.up));
      });
      builder.HStack(stack =>
      {
        var left = stack.AddIconButton(arrowUp, () => LoaderManager.Move(Direction.left));
        left.localEulerAngles += new Vector3(0, 0, 90);
        var down = stack.AddIconButton(arrowUp, () => LoaderManager.Move(Direction.backward));
        down.localEulerAngles += new Vector3(0, 0, 180);
        var right = stack.AddIconButton(arrowUp, () => LoaderManager.Move(Direction.right));
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
        stack.AddIconButton(zNeg, () => LoaderManager.Rotate(Vector3.back));
        stack.AddIconButton(xPos, () => LoaderManager.Rotate(Vector3.right));
        stack.AddIconButton(zPos, () => LoaderManager.Rotate(Vector3.forward));
      });
      builder.HStack(stack =>
      {
        stack.AddIconButton(yNeg, () => LoaderManager.Rotate(Vector3.down));
        stack.AddIconButton(xNeg, () => LoaderManager.Rotate(Vector3.left));
        stack.AddIconButton(yPos, () => LoaderManager.Rotate(Vector3.up));
      });
    }


    protected override void BeforeWindowShown()
    {
      base.BeforeWindowShown();
    }

    protected override void AfterWindowClosed()
    {
      base.AfterWindowClosed();
      EditorContext.SelectedNode = null;
    }

  }
}
