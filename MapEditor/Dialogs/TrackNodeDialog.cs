using System.Collections.Generic;
using MapEditor.Extensions;
using MapEditor.Managers;
using Serilog;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace MapEditor.Dialogs
{
  public sealed class TrackNodeDialog : DialogBase
  {

    public TrackNodeDialog() : base("Node editor", 400, 300, Window.Position.CenterRight)
    {
    }

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Position", () => EditorContext.SelectedNode?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Rotation", () => EditorContext.SelectedNode?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.Spacer();
      builder.HStack(stack =>
      {
        stack.AddSection("Position", BuildPositionEditor);
        stack.Spacer();
        stack.AddSection("Rotation", BuildRotationEditor);
      });
      builder.AddSection("Scaling", BuildScalingEditor);
      builder.AddSection("Nodes", BuildNodeEditor);
      builder.AddSection("Segments", BuildSegmentEditor);
      builder.AddExpandingVerticalSpacer();
    }

    private static void BuildPositionEditor(UIPanelBuilder builder)
    {
      var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"- {NodeManager.Scaling:F2}", () => NodeManager.Move(Direction.down));
        stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.forward));
        stack.AddButtonCompact(() => $"+ {NodeManager.Scaling:F2}", () => NodeManager.Move(Direction.up));
      });
      builder.HStack(stack =>
      {
        var left = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.left));
        left.localEulerAngles += new Vector3(0, 0, 90);
        var down = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.backward));
        down.localEulerAngles += new Vector3(0, 0, 180);
        var right = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.right));
        right.localEulerAngles += new Vector3(0, 0, -90);
      });
    }

    private static void BuildRotationEditor(UIPanelBuilder builder)
    {
      var xpos = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 256, 256, -256), new Vector2(0.5f, 0.5f))!;
      var xneg = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var ypos = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var yneg = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var zpos = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var zneg = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack =>
      {
        stack.AddIconButton(zneg, () => NodeManager.Rotate(Vector3.back));
        stack.AddIconButton(xpos, () => NodeManager.Rotate(Vector3.right));
        stack.AddIconButton(zpos, () => NodeManager.Rotate(Vector3.forward));
      });
      builder.HStack(stack =>
      {
        stack.AddIconButton(yneg, () => NodeManager.Rotate(Vector3.down));
        stack.AddIconButton(xneg, () => NodeManager.Rotate(Vector3.left));
        stack.AddIconButton(ypos, () => NodeManager.Rotate(Vector3.up));
      });
    }

    private static void BuildScalingEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"+{NodeManager.ScalingDelta:0.##}", NodeManager.IncrementScaling);
        stack.AddButtonCompact(() => "0", NodeManager.ResetScaling);
        stack.AddButtonCompact(() => $"-{NodeManager.ScalingDelta:0.##}", NodeManager.DecrementScaling);
        stack.Spacer();
        stack.AddButtonCompact(() => "0.01", () => NodeManager.ScalingDelta = 0.01f);
        stack.AddButtonCompact(() => "0.1", () => NodeManager.ScalingDelta = 0.1f);
        stack.AddButtonCompact(() => "1", () => NodeManager.ScalingDelta = 1f);
        stack.AddButtonCompact(() => "10", () => NodeManager.ScalingDelta = 10f);
      });
    }

    private static void BuildNodeEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack =>
      {
        stack.AddButtonCompact("Flip", NodeManager.FlipSwitchStand);
        stack.AddButtonCompact("Add", NodeManager.AddNode);
        stack.AddButtonCompact("Split", NodeManager.SplitNode);
        stack.AddButtonCompact("Remove", () => NodeManager.RemoveNode(Input.GetKey(KeyCode.LeftShift)));
        stack.AddPopupMenu(
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
      if (node == null)
      {
        return;
      }

      var segments = Graph.Shared.SegmentsAffectedByNodes(new HashSet<TrackNode> { node });

      builder.HStack(stack =>
      {
        foreach (var segment in segments)
        {
          stack.AddButtonCompact(segment.id, () => EditorContext.SelectedSegment = segment);
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
      _logger.Information("AfterWindowClosed");
      base.AfterWindowClosed();
      EditorContext.SelectedNode = null;
    }

    private static readonly ILogger _logger = Log.ForContext(typeof(TrackNodeDialog));

  }
}
