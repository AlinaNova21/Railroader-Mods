using System;
using System.Linq;
using MapEditor.StateTracker;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Tools
{
  public class UIMoveTool : BaseTool
  {
    protected override string ToolIconPath => "Icons/MoveTool";
    protected override string ToolName => "Move";
    protected override string ToolDescription => "Move objects";

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
      var rotate = (Vector3 offset) => {
        var node = Context.SelectedNode;
        Context.ChangeManager.AddChange(new TrackNodeChanged(node, node.transform.localPosition, node.transform.localEulerAngles + offset, node.flipSwitchStand));
      };

      var move = (Vector3 offset) => {
        var node = Context.SelectedNode;
        Context.ChangeManager.AddChange(new TrackNodeChanged(node, node.transform.localPosition + offset, node.transform.localEulerAngles, node.flipSwitchStand));
      };

      var flipSwitchStand = () => {
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
          builder.HStack(builder =>
          {
            builder.AddButtonCompact(() => $"- {scaling:F2}", () => move(Context.SelectedNode.transform.up * -scaling));
            builder.AddButtonCompact(() => $"^ {scaling:F2}", () => move(Context.SelectedNode.transform.forward * scaling));
            builder.AddButtonCompact(() => $"+ {scaling:F2}", () => move(Context.SelectedNode.transform.up * scaling));
          });
          builder.HStack(builder =>
          {
            builder.AddButtonCompact(() => $"< {scaling:F2}", () => move(Context.SelectedNode.transform.right * -scaling));
            builder.AddButtonCompact(() => $"v {scaling:F2}", () => move(Context.SelectedNode.transform.forward * -scaling));
            builder.AddButtonCompact(() => $"> {scaling:F2}", () => move(Context.SelectedNode.transform.right * scaling));
          });
        });
        builder.Spacer();
        builder.AddSection("Rotation", builder =>
        {
          builder.HStack(builder =>
          {
            builder.AddButtonCompact(() => $"Z -{scaling:F2}", () => rotate(Vector3.forward * -scaling));
            builder.AddButtonCompact(() => $"X +{scaling:F2}", () => rotate(Vector3.right * scaling));
            builder.AddButtonCompact(() => $"Z +{scaling:F2}", () => rotate(Vector3.forward * scaling));
          });
          builder.HStack(builder =>
          {
            builder.AddButtonCompact(() => $"Y -{scaling:F2}", () => rotate(Vector3.up * -scaling));
            builder.AddButtonCompact(() => $"X -{scaling:F2}", () => rotate(Vector3.right * -scaling));
            builder.AddButtonCompact(() => $"Y +{scaling:F2}", () => rotate(Vector3.up * scaling));
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
          builder.AddButtonCompact("0.05", () => scaling = 0.01f);
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
          var newNode = Graph.Shared.GetNode(nid);
          var segmentCreated = new TrackSegmentCreated(sid, node, newNode);
          var compoundChange = new CompoundChange(nodeCreated, segmentCreated);
          Context.ChangeManager.AddChange(compoundChange);
          TrackObjectManager.Instance.Rebuild();
          Context.SelectNode(newNode);
        });
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
      if (Window.IsShown == false) {
        Window.ShowWindow();
      }
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