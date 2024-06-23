using System;
using JetBrains.Annotations;
using MapEditor.Extensions;
using MapEditor.Managers;
using MapEditor.StateTracker.Segment;
using Serilog;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs
{
  public class TrackSegmentDialog() : DialogBase("Segment editor", 400, 350, Window.Position.LowerRight)
  {

    private UIPanelBuilder? _Builder;
    private float _SpeedLimit;

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      var segment = EditorContext.SelectedSegment;
      if (segment == null)
      {
        return;
      }

      _Builder = builder;
      _SpeedLimit = Mathf.Floor(segment.speedLimit / 5f);


      builder.AddSection("Properties", section => {
        section.AddField("Priority", builder.AddInputFieldValidated($"{segment.priority}", value => SegmentManager.UpdatePriority(int.Parse(value)), "\\d+")!);
        section.AddField("Speed Limit", () => $"{_SpeedLimit * 5}", UIPanelBuilder.Frequency.Periodic);
        section.AddSlider(() => _SpeedLimit, () => $"{_SpeedLimit * 5}", o => _SpeedLimit = o, 0, 9, true, o => SegmentManager.UpdateSpeedLimit((int)o * 5));
        section.AddField("Group ID", builder.AddInputField(segment.groupId ?? "", SegmentManager.UpdateGroup, "groupId")!);
        section.AddField("Track style", builder.AddEnumDropdown(segment.style, SegmentManager.UpdateStyle));
        section.AddField("Track class", builder.AddEnumDropdown(segment.trackClass, SegmentManager.UpdateTrackClass));
      });
      builder.Spacer();
      builder.AddSection("Position", BuildPositionEditor);
      builder.AddSection("Scaling", Utility.BuildScalingEditor);
      builder.Spacer();
      builder.HStack(stack =>
      {
        stack.AddButtonCompact("Remove", SegmentManager.RemoveSegment);
      });
      builder.AddField("Nodes", builder.HStack(stack =>
      {
        stack.AddButtonCompact(segment?.a.id ?? "", () =>
        {
          var node = segment!.a;
          EditorContext.SelectedSegment = null;
          EditorContext.SelectedNode = node;
        });
        stack.AddButtonCompact(segment?.b.id ?? "", () =>
        {
          var node = segment!.b;
          EditorContext.SelectedSegment = null;
          EditorContext.SelectedNode = node;
        });
      })!);
    }

    private static void BuildPositionEditor(UIPanelBuilder builder)
    {
      builder.HStack(stack =>
      {
        stack.AddButtonCompact(() => $"- {NodeManager.Scaling:F2}", () => SegmentManager.Move(Direction.down));
        stack.AddButtonCompact(() => $"+ {NodeManager.Scaling:F2}", () => SegmentManager.Move(Direction.up));
      });
    }

    protected override void BeforeWindowShown()
    {
      _Builder?.Rebuild();
      base.BeforeWindowShown();
    }

    protected override void AfterWindowClosed()
    {
      base.AfterWindowClosed();
      EditorContext.SelectedSegment = null;
    }

    [UsedImplicitly]
    public void Activate()
    {
    }

    [UsedImplicitly]
    public void Deactivate()
    {
      CloseWindow();
    }

  }
}
