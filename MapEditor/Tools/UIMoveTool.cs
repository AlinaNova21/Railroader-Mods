using System;
using System.Collections.Generic;
using TMPro;
using UI.CompanyWindow;

namespace MapEditor.Tools {
  using MapEditor.Extensions;
  using MapEditor.Managers;
  using MapEditor.StateTracker.Segment;
  using Track;
  using UI.Builder;
  using UI.Common;
  using UnityEngine;
  using Resources = MapEditor.Resources;

  public class UIMoveTool : BaseTool {

    protected override string ToolIconPath => "Icons/MoveTool";
    protected override string ToolName => "Move";
    protected override string ToolDescription => "Move objects";

    private TrackNode? PreviousNode { get; set; }

    public Window Window { get; }

    public UIMoveTool() {
      Window = EditorMod.Shared.UIHelper.CreateWindow(400, 300, Window.Position.CenterRight);
      EditorMod.Shared.UIHelper.PopulateWindow(Window, BuildWindow);
      Window.OnShownDidChange += shown => {
        if (!shown && Context.ActiveTool == this) {
          Deactivate();
        }
      };
    }

    private void BuildWindow(UIPanelBuilder builder) {
      builder.AddField("Keyboard mode", () => KeyboardManager.Rotate ? "rotate" : "move", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Position", () => Context?.SelectedNode?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Rotation", () => Context?.SelectedNode?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.Spacer();
      builder.HStack(stack => {
        stack.AddSection("Position", BuildPositionEditor);
        stack.Spacer();
        stack.AddSection("Rotation", BuildRotationEditor);
      });
      builder.AddSection("Scaling", BuildScalingEditor);
      builder.AddSection("Nodes", BuildNodeEditor);
      builder.AddSection("Segments", BuildSegmentEditor);
    }

    private void BuildPositionEditor(UIPanelBuilder builder) {
      var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack => {
        stack.AddButtonCompact(() => $"- {NodeManager.Scaling:F2}", () => NodeManager.Move(Direction.down));
        stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.forward));
        stack.AddButtonCompact(() => $"+ {NodeManager.Scaling:F2}", () => NodeManager.Move(Direction.up));
      });
      builder.HStack(stack => {
        var left = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.left));
        left.localEulerAngles += new Vector3(0, 0, 90);
        var down = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.backward));
        down.localEulerAngles += new Vector3(0, 0, 180);
        var right = stack.AddIconButton(arrowUp, () => NodeManager.Move(Direction.right));
        right.localEulerAngles += new Vector3(0, 0, -90);
      });
    }

    private void BuildRotationEditor(UIPanelBuilder builder) {
      var xpos = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 256, 256, -256), new Vector2(0.5f, 0.5f))!;
      var xneg = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var ypos = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var yneg = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      var zpos = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f))!;
      var zneg = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f))!;
      builder.HStack(stack => {
        stack.AddIconButton(zneg, () => NodeManager.Rotate(Vector3.back));
        stack.AddIconButton(xpos, () => NodeManager.Rotate(Vector3.right));
        stack.AddIconButton(zpos, () => NodeManager.Rotate(Vector3.forward));
      });
      builder.HStack(stack => {
        stack.AddIconButton(yneg, () => NodeManager.Rotate(Vector3.down));
        stack.AddIconButton(xneg, () => NodeManager.Rotate(Vector3.left));
        stack.AddIconButton(ypos, () => NodeManager.Rotate(Vector3.up));
      });
    }

    private void BuildScalingEditor(UIPanelBuilder builder) {
      builder.HStack(stack => {
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
    
    private void BuildNodeEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack => {
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

    private void BuildSegmentEditor(UIPanelBuilder builder)
    {
      EditorContext.NodeSelectedChanged += _ =>  builder.Rebuild();

      var node = Context?.SelectedNode;
      if (node == null)
      {
        return;
      }

      var segments = Graph.Shared.SegmentsAffectedByNodes(new HashSet<TrackNode> { node });

      builder.HStack(stack =>
      {
        foreach (var segment in segments)
        {
          stack.AddButtonCompact(segment.id, () => Context.SelectSegment(segment));
        }
      });
    }


    private void SelectedNodeChanged(TrackNode? node) {
      if (node == null) {
        Window.CloseWindow();
        return;
      }

      Window.Title = $"Node Editor - {node.id}";
      if (Window.IsShown == false) {
        Window.ShowWindow();
      }

      if (Input.GetKey(KeyCode.LeftShift) && PreviousNode != null) {
        NodeManager.ConnectNodes(PreviousNode.id!);
      }

      PreviousNode = node;
    }

    public override void OnActivated() {
      SelectedNodeChanged(Context.SelectedNode);
      EditorContext.NodeSelectedChanged += SelectedNodeChanged;
    }

    protected override void OnDeactivating() {
      Window.CloseWindow();
      // NOTE: im unable to reopen window if this is not commented out
      //EditorContext.NodeSelectedChanged -= SelectedNodeChanged;
    }

  }
}
