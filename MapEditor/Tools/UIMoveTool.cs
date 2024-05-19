using System;
using System.Linq;
using HarmonyLib;
using MapEditor.Extensions;
using MapEditor.StateTracker;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor.Tools
{
  public class UIMoveTool : BaseTool
  {
    protected override string ToolIconPath => "Icons/MoveTool";
    protected override string ToolName => "Move";
    protected override string ToolDescription => "Move objects";

    private TrackNode PreviousNode { get; set; }

    private Window Window { get; set; }

    private float scaling = 1.0f;

    public UIMoveTool()
    {
      Window = EditorMod.Shared.UIHelper.CreateWindow(400, 200, UI.Common.Window.Position.CenterRight);
      EditorMod.Shared.UIHelper.PopulateWindow(Window, builder => BuildWindow(builder));
      Window.OnShownDidChange += (shown) =>
      {
        if (!shown && Context.ActiveTool == this)
        {
          Deactivate();
        }
      };
    }

    private void BuildWindow(UIPanelBuilder builder)
    {
      var rotate = (Vector3 offset) =>
      {
        var node = Context.SelectedNode;
        Context.ChangeManager.AddChange(new TrackNodeChanged(node, node.transform.localPosition, node.transform.localEulerAngles + offset, node.flipSwitchStand));
      };

      var move = (Vector3 offset) =>
      {
        var node = Context.SelectedNode;
        Context.ChangeManager.AddChange(new TrackNodeChanged(node, node.transform.localPosition + offset, node.transform.localEulerAngles, node.flipSwitchStand));
      };

      var flipSwitchStand = () =>
      {
        var node = Context.SelectedNode;
        Context.ChangeManager.AddChange(new TrackNodeChanged(node, node.transform.localPosition, node.transform.localEulerAngles, !node.flipSwitchStand));
      };

      builder.AddField("Position", () => Context?.SelectedNode?.transform.localPosition.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.AddField("Rotation", () => Context?.SelectedNode?.transform.localEulerAngles.ToString() ?? "", UIPanelBuilder.Frequency.Periodic);
      builder.Spacer();
      builder.HStack(builder =>
      {
        builder.AddSection("Position", builder =>
        {
          var arrowUp = Sprite.Create(Resources.Icons.ArrowUp, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
          builder.HStack(builder =>
          {
            builder.AddButtonCompact(() => $"- {scaling:F2}", () => move(Context.SelectedNode.transform.up * -scaling));
            var up = builder.AddIconButton(arrowUp, () => move(Context.SelectedNode.transform.forward * scaling));
            builder.AddButtonCompact(() => $"+ {scaling:F2}", () => move(Context.SelectedNode.transform.up * scaling));
          });
          builder.HStack(builder =>
          {
            var left = builder.AddIconButton(arrowUp, () => move(Context.SelectedNode.transform.right * -scaling));
            left.localEulerAngles += new Vector3(0, 0, 90);
            var down = builder.AddIconButton(arrowUp, () => move(Context.SelectedNode.transform.forward * -scaling));
            down.localEulerAngles += new Vector3(0, 0, 180);
            var right = builder.AddIconButton(arrowUp, () => move(Context.SelectedNode.transform.right * scaling));
            right.localEulerAngles += new Vector3(0, 0, -90);
          });
        });
        builder.Spacer();
        builder.AddSection("Rotation", builder =>
        {
          var xpos = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 256, 256, -256), new Vector2(0.5f, 0.5f));
          var xneg = Sprite.Create(Resources.Icons.RotateAxisX, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
          var ypos = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f));
          var yneg = Sprite.Create(Resources.Icons.RotateAxisY, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
          var zpos = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(256, 0, -256, 256), new Vector2(0.5f, 0.5f));
          var zneg = Sprite.Create(Resources.Icons.RotateAxisZ, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
          builder.HStack(builder =>
          {
            builder.AddIconButton(zneg, () => rotate(Vector3.forward * -scaling));
            builder.AddIconButton(xpos, () => rotate(Vector3.right * scaling));
            builder.AddIconButton(zpos, () => rotate(Vector3.forward * scaling));
          });
          builder.HStack(builder =>
          {
            builder.AddIconButton(yneg, () => rotate(Vector3.up * -scaling));
            builder.AddIconButton(xneg, () => rotate(Vector3.right * -scaling));
            builder.AddIconButton(ypos, () => rotate(Vector3.up * scaling));
          });
        });
      });
      builder.AddSection("Scaling", builder =>
      {
        builder.HStack(builder =>
        {
          builder.AddButtonCompact("10", () => scaling = 10.0f);
          builder.AddButtonCompact("5", () => scaling = 5.0f);
          builder.AddButtonCompact("1", () => scaling = 1.0f);
          builder.AddButtonCompact("0.5", () => scaling = 0.5f);
          builder.AddButtonCompact("0.1", () => scaling = 0.1f);
          builder.AddButtonCompact("0.01", () => scaling = 0.01f);
        });
      });

      builder.HStack(builder =>
      {
        builder.AddButtonCompact("Flip Switch Stand", () => flipSwitchStand());
        builder.AddButtonCompact("Add Node", () =>
        {
          var node = Context.SelectedNode;
          var nid = Context.TrackNodeIdGenerator.Next();
          var sid = Context.TrackSegmentIdGenerator.Next();
          var nodeCreated = new TrackNodeCreated(nid, node.transform.localPosition + (node.transform.forward * scaling), node.transform.localEulerAngles);
          Context.ChangeManager.AddChange(nodeCreated);
          var newNode = Graph.Shared.GetNode(nid);
          var segmentCreated = new TrackSegmentCreated(sid, node, newNode);
          Context.ChangeManager.AddChange(segmentCreated);
          // var compoundChange = new CompoundChange(nodeCreated, segmentCreated);
          // Context.ChangeManager.AddChange(compoundChange);
          TrackObjectManager.Instance.Rebuild();
          Context.SelectNode(newNode);
        });
        // builder.AddButtonCompact("Delete Node", () =>
        // {
        //   var node = Context.SelectedNode;
        //   if (node == null)
        //   {
        //     return;
        //   }
        //   var segments = Graph.Shared.SegmentsConnectedTo(node).ToArray();
        //   foreach (var segment in segments)
        //   {
        //     Context.ChangeManager.AddChange(new TrackSegmentDeleted(segment.id, segment.a, segment.b, segment.style, segment.groupId));
        //   }
        //   var nodeDeleted = new TrackNodeDeleted(node.id, node.transform.localPosition, node.transform.localEulerAngles, node.flipSwitchStand);
        //   Context.ChangeManager.AddChange(nodeDeleted);
        //   EditorContext.Instance.SelectNode(null);
        //   TrackObjectManager.Instance.Rebuild();
        // });
      });
    }

    private void SelectedNodeChanged(TrackNode node)
    {
      if (node == null)
      {
        Window.CloseWindow();
        return;
      }
      Window.Title = $"Node Editor - {node.id}";
      if (Window.IsShown == false)
      {
        Window.ShowWindow();
      }
      if (Input.GetKey(KeyCode.LeftShift) && PreviousNode != null)
      {
        var sid = Context.TrackSegmentIdGenerator.Next();
        var segmentCreated = new TrackSegmentCreated(sid, PreviousNode, node);
        Context.ChangeManager.AddChange(segmentCreated);
        TrackObjectManager.Instance.Rebuild();
      }
      PreviousNode = node;
    }

    public override void OnActivated()
    {
      SelectedNodeChanged(Context.SelectedNode);
      EditorContext.NodeSelectedChanged += SelectedNodeChanged;
    }

    protected override void OnDeactivating()
    {
      Window.CloseWindow();
      EditorContext.NodeSelectedChanged -= SelectedNodeChanged;
    }
  }

}
