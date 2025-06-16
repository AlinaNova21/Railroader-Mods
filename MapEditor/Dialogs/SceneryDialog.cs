using Helpers;
using MapEditor.Extensions;
using MapEditor.Managers;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs;

public sealed class SceneryDialog() : DialogBase("SceneryEditor", "Node editor", 450, 450, Window.Position.CenterRight)
{

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Position", () => EditorContext.SelectedScenery?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Rotation", () => EditorContext.SelectedScenery?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.Spacer();
    builder.HStack(stack => {
      stack.AddSection("Position", builder => Utility.BuildPositionEditor(builder, dir => SceneryManager.Move(dir)));
      stack.Spacer();
      stack.AddSection("Rotation", builder => Utility.BuildRotationEditor(builder, off => SceneryManager.Rotate(off)));
    });
    builder.Spacer();
    builder.AddSection("Scaling", Utility.BuildScalingEditor);
    builder.AddSection("Scenery", BuildSceneryEditor);
    builder.AddExpandingVerticalSpacer();
  }

  private static void BuildSceneryEditor(UIPanelBuilder builder)
  {
    builder.HStack(stack => {
      stack.AddButtonCompact("Remove", () => SceneryManager.RemoveScenery());
      stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedScenery);
    });
    builder.Spacer();
    var sceneryList = SceneryAssetManager.Shared.GetSceneryDefinitionIdentifiers();
    sceneryList.Sort();
    var selectedSceneryIndex = sceneryList.IndexOf(EditorContext.SelectedScenery?.identifier);
    builder.AddDropdown(sceneryList, selectedSceneryIndex, (index) => {
      var scenery = sceneryList[index];
      SceneryManager.Model(scenery);
    });
    builder.HStack(stack => {
      if (EditorContext.SelectedTransformableObject?.CanDestroy ?? false)
        stack.AddButtonCompact("Remove", () => SceneryManager.RemoveScenery());
      if (EditorContext.SelectedTransformableObject?.CanMove ?? false)
        stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedScenery);
      stack.AddPopupMenu("More ...",
        new PopupMenuItem("Copy rotation", SceneryManager.CopyRotation),
        new PopupMenuItem("Paste rotation", SceneryManager.PasteRotation),
        new PopupMenuItem("Copy elevation", SceneryManager.CopyElevation),
        new PopupMenuItem("Paste elevation", SceneryManager.PasteElevation)
      );
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
