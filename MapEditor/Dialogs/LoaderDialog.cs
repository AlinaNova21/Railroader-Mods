using System.Collections.Generic;
using MapEditor.Managers;
using Model.Ops;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs;

public sealed class LoaderDialog() : DialogBase("loaderEditor", "Node editor", 400, 350, Window.Position.CenterRight)
{

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Position", () => EditorContext.SelectedLoader?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Rotation", () => EditorContext.SelectedLoader?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.Spacer();
    builder.HStack(stack => {
      stack.AddSection("Position", builder => Utility.BuildPositionEditor(builder, dir => LoaderManager.Move(dir)));
      stack.Spacer();
      stack.AddSection("Rotation", builder => Utility.BuildRotationEditor(builder, off => LoaderManager.Rotate(off)));
    });
    builder.AddSection("Scaling", Utility.BuildScalingEditor);
    builder.AddSection("Loader", BuildLoaderEditor);
    builder.AddExpandingVerticalSpacer();
  }

  private static void BuildLoaderEditor(UIPanelBuilder builder)
  {
    builder.HStack(stack => {
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
    var selectedValue = EditorContext.SelectedLoader?.Prefab.Replace("vanilla://", "") ?? "";
    var selectedIndex = prefabs.IndexOf(selectedValue);
    builder.AddDropdown(prefabs, selectedIndex, (index) => {
      var prefab = prefabs[index];
      LoaderManager.Prefab("vanilla://" + prefab);
    });
    var industries = new List<string>();
    var allIndustries = GameObject.FindObjectsOfType<Industry>();
    foreach (var industry in allIndustries) {
      industries.Add(industry.identifier);
    }
    var industrySelectedIndex = industries.IndexOf(EditorContext.SelectedLoader!.Industry);
    industries.Sort();
    builder.AddDropdown(industries, industrySelectedIndex, (index) => {
      var industry = industries[index];
      LoaderManager.Industry(industry);
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
