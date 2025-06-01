using MapEditor.Extensions;
using MapEditor.Managers;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs;

public sealed class ObjectDialog() : DialogBase("ObjectEditor", "Node editor", 450, 600, Window.Position.CenterRight)
{

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Type", () => EditorContext.SelectedObject?.DisplayType ?? "Unknown", UIPanelBuilder.Frequency.Periodic);
    var transformable = EditorContext.SelectedTransformableObject;
    if (transformable != null) {
      if (transformable.CanMove) {
        builder.AddField("Position", () => transformable.Position.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      }
      if (transformable.CanRotate) {
        builder.AddField("Rotation", () => transformable.Rotation.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      }
      builder.Spacer();
      builder.HStack(stack => {
        if (transformable.CanMove)
          stack.AddSection("Position", builder => Utility.BuildPositionEditor(builder, dir => ObjectManager.Move(dir)));
        else
          builder.AddLabel("Cannot Move");
        stack.Spacer();
        if (transformable.CanRotate)
          stack.AddSection("Rotation", builder => Utility.BuildRotationEditor(builder, off => ObjectManager.Rotate(off)));
        else
          builder.AddLabel("Cannot Rotate");
      });
      builder.AddSection("Adjustment Scaling", Utility.BuildScalingEditor);
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
