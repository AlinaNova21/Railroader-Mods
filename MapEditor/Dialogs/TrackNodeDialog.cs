using MapEditor.Extensions;
using MapEditor.Managers;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs;

public sealed class TrackNodeDialog() : DialogBase("nodeeditor", "Node editor", 450, 500, Window.Position.CenterRight)
{

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Position", () => EditorContext.SelectedNode?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.AddField("Rotation", () => EditorContext.SelectedNode?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
    builder.Spacer(25);
    builder.HStack(stack => {
      stack.AddSection("Position", builder => Utility.BuildPositionEditor(builder, dir => NodeManager.Move(dir)));
      stack.Spacer();
      stack.AddSection("Rotation", builder => Utility.BuildRotationEditor(builder, off => NodeManager.Rotate(off)));
    });
    builder.Spacer(25);
    builder.AddSection("Scaling", Utility.BuildScalingEditor);
    builder.AddSection("Nodes", BuildNodeEditor);
    builder.Spacer(25);
    builder.AddSection("Segments", BuildSegmentEditor);
    builder.AddExpandingVerticalSpacer();
  }

  private static void BuildNodeEditor(UIPanelBuilder builder)
  {
    builder.AddField("Flip Switch Stand", builder.AddToggle(() => NodeManager.GetFlipSwitchStand(), val => NodeManager.FlipSwitchStand(val))!);
    builder.HStack(stack => {
      stack.AddButtonCompact("Add", () => NodeManager.AddNode());
      stack.AddButtonCompact("Split", () => NodeManager.SplitNode());
      stack.AddButtonCompact("Level", () => NodeManager.LevelNode());
      stack.AddButtonCompact("Flip", () => NodeManager.FlipNode());
    });
    builder.HStack(stack => {
      stack.AddButtonCompact("Remove", () => NodeManager.RemoveNode(Input.GetKey(KeyCode.LeftShift)));
      stack.AddButtonCompact("Show", EditorContext.MoveCameraToSelectedNode);
      stack.AddPopupMenu("More ...",
        new PopupMenuItem("Copy rotation", NodeManager.CopyNodeRotation),
        new PopupMenuItem("Paste rotation", NodeManager.PasteNodeRotation),
        new PopupMenuItem("Copy elevation", NodeManager.CopyNodeElevation),
        new PopupMenuItem("Paste elevation", NodeManager.PasteNodeElevation)
      );
    });
  }

  private UIPanelBuilder? _SegmentEditorBuilder;

  private void BuildSegmentEditor(UIPanelBuilder builder)
  {
    _SegmentEditorBuilder = builder;

    var node = EditorContext.SelectedNode;
    if (node == null) {
      return;
    }

    var segments = Graph.Shared.SegmentsAffectedByNodes([node])!;

    builder.VStack(stack => {
      foreach (var segment in segments) {
        stack.AddButtonCompact(segment.id, () => {
          EditorContext.SelectedSegment = segment;
        });
      }
    });
  }

  protected override void BeforeWindowShown()
  {
    base.BeforeWindowShown();
    _SegmentEditorBuilder?.Rebuild();
  }

  protected override void AfterWindowClosed()
  {
    base.AfterWindowClosed();
    EditorContext.SelectedNode = null;
  }
}
