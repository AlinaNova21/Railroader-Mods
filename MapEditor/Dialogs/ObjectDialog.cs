using System.Linq;
using MapEditor.Extensions;
using MapEditor.Managers;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs;

public sealed class ObjectDialog() : DialogBase("ObjectEditor", "Node editor", 450, 600, Window.Position.CenterRight)
{

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Type", () => EditorContext.SelectedObject?.DisplayType ?? "Unknown", UIPanelBuilder.Frequency.Periodic);
    var transformables = EditorContext.SelectedTransformableObjects;
    if (transformables.Any()) {
      if (transformables.All(s => s.CanMove)) {
        builder.AddField("Position", () => transformables[0].Position.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      }
      if (transformables.All(s => s.CanRotate)) {
        builder.AddField("Rotation", () => transformables[0].Rotation.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      }
      builder.Spacer();
      builder.HStack(stack => {
        if (transformables.All(s => s.CanMove))
          stack.AddSection("Position", builder => Utility.BuildPositionEditor(builder, dir => ObjectManager.Move(dir)));
        else
          builder.AddLabel("Cannot Move");
        stack.Spacer();
        if (transformables.All(s => s.CanRotate))
          stack.AddSection("Rotation", builder => Utility.BuildRotationEditor(builder, off => ObjectManager.Rotate(off)));
        else
          builder.AddLabel("Cannot Rotate");
      });
      builder.Spacer(10);
      builder.AddSection("Adjustment Scaling", Utility.BuildScalingEditor);
      if (transformables.Count > 1 && transformables.All(s => s.CanMove)) {
        builder.AddSection("Alignment");
        builder.HStack(stack => {
          stack.AddButtonCompact("Align X", () => ObjectManager.Align(Vector3.right));
          stack.AddButtonCompact("Align Z", () => ObjectManager.Align(Vector3.forward));
        });
      }
    }
    builder.Spacer(10);
    builder.AddSection(EditorContext.SelectedObject?.DisplayType ?? "Object", builder => EditorContext.SelectedObject?.BuildUI(builder, UIHelper.Instance));
    builder.Spacer(10);
    builder.HStack(stack => {
      if (EditorContext.SelectedTransformableObject?.CanDestroy ?? false)
        stack.AddButtonCompact("Remove", () => ObjectManager.Remove());
      if (EditorContext.SelectedTransformableObject?.CanMove ?? false)
        stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedObject);
      stack.AddPopupMenu("More ...",
        new PopupMenuItem("Copy rotation", ObjectManager.CopyRotation),
        new PopupMenuItem("Paste rotation", ObjectManager.PasteRotation),
        new PopupMenuItem("Copy elevation", ObjectManager.CopyElevation),
        new PopupMenuItem("Paste elevation", ObjectManager.PasteElevation)
      );
    });
    builder.AddExpandingVerticalSpacer();
  }

  protected override void BeforeWindowShown()
  {
    base.BeforeWindowShown();
    Rebuild();
  }

  protected override void AfterWindowClosed()
  {
    base.AfterWindowClosed();
    EditorContext.SelectedObject = null;
  }
}
