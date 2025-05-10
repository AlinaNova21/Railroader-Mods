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
  public sealed class SceneryDialog() : DialogBase("SceneryEditor", "Node editor", 400, 350, Window.Position.CenterRight)
  {

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Position", () => EditorContext.SelectedScenery?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Rotation", () => EditorContext.SelectedScenery?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.Spacer();
      builder.HStack(stack =>
      {
        stack.AddSection("Position", BuildPositionEditor);
        stack.Spacer();
        stack.AddSection("Rotation", BuildRotationEditor);
      });
      builder.AddSection("Scaling", Utility.BuildScalingEditor);
      builder.AddSection("Scenery", BuildSceneryEditor);
      builder.AddExpandingVerticalSpacer();
    }

    private static void BuildSceneryEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack =>
      {
        stack.AddButtonCompact("Remove", () => SceneryManager.RemoveScenery());
        stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedScenery);
      });
      builder.Spacer();
      var sceneryList = SceneryAssetManager.Shared.GetSceneryDefinitionIdentifiers();
      sceneryList.Sort();
      var selectedSceneryIndex = sceneryList.IndexOf(EditorContext.SelectedScenery?.identifier);
      builder.AddDropdown(sceneryList, selectedSceneryIndex, (index) =>
      {
        var scenery = sceneryList[index];
        SceneryManager.Model(scenery);
      });
      //var prefabs = new List<string>
      //{
      //  "coalConveyor",
      //  "coalTower",
      //  "dieselFuelingStand",
      //  "waterTower",
      //  "waterColumn"
      //};
      //var selectedValue = EditorContext.SelectedScenery?.config.Prefab.Replace("vanilla://", "");
      //var selectedIndex = prefabs.IndexOf(selectedValue);
      //builder.AddDropdown(prefabs, selectedIndex, (index) =>
      //{
      //  var prefab = prefabs[index];
      //  SceneryManager.Prefab("vanilla://" + prefab);
      //});
      //var industries = new List<string>();
      //var allIndustries = GameObject.FindObjectsOfType<Industry>();
      //foreach (var industry in allIndustries)
      //{
      //  industries.Add(industry.identifier);
      //}
      //var industrySelectedIndex = industries.IndexOf(EditorContext.SelectedScenery!.config.Industry);
      //industries.Sort();
      //builder.AddDropdown(industries, industrySelectedIndex, (index) =>
      //{
      //  var industry = industries[index];
      //  SceneryManager.Industry(industry);
      //});
    }

    private static void BuildPositionEditor(UIPanelBuilder builder)
    {
      var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"- {SceneryManager.Scaling:F2}", () => SceneryManager.Move(Direction.down));
        stack.AddIconButton(arrowUp, () => SceneryManager.Move(Direction.forward));
        stack.AddButtonCompact(() => $"+ {SceneryManager.Scaling:F2}", () => SceneryManager.Move(Direction.up));
      });
      builder.HStack(stack =>
      {
        var left = stack.AddIconButton(arrowUp, () => SceneryManager.Move(Direction.left));
        left.localEulerAngles += new Vector3(0, 0, 90);
        var down = stack.AddIconButton(arrowUp, () => SceneryManager.Move(Direction.backward));
        down.localEulerAngles += new Vector3(0, 0, 180);
        var right = stack.AddIconButton(arrowUp, () => SceneryManager.Move(Direction.right));
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
        stack.AddIconButton(zNeg, () => SceneryManager.Rotate(Vector3.back));
        stack.AddIconButton(xPos, () => SceneryManager.Rotate(Vector3.right));
        stack.AddIconButton(zPos, () => SceneryManager.Rotate(Vector3.forward));
      });
      builder.HStack(stack =>
      {
        stack.AddIconButton(yNeg, () => SceneryManager.Rotate(Vector3.down));
        stack.AddIconButton(xNeg, () => SceneryManager.Rotate(Vector3.left));
        stack.AddIconButton(yPos, () => SceneryManager.Rotate(Vector3.up));
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
