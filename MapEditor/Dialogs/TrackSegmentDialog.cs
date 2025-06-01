using JetBrains.Annotations;
using MapEditor.Extensions;
using MapEditor.Managers;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.Dialogs;

public class TrackSegmentDialog() : DialogBase("segmenteditor", "Segment editor", 400, 500, Window.Position.LowerRight)
{

  private UIPanelBuilder? _Builder;
  private float _SpeedLimit;

  protected override void BuildWindow(UIPanelBuilder builder)
  {
    var segment = EditorContext.SelectedSegment;
    if (segment == null) {
      return;
    }

    _Builder = builder;
    _SpeedLimit = Mathf.Floor(segment.speedLimit / 5f);

    builder.AddSection("Properties", section => {
      section.AddField("Priority", builder.AddInputField($"{segment.priority}", value => SegmentManager.UpdatePriority(int.Parse(value)))!);
      section.AddField("Speed Limit", () => $"{_SpeedLimit * 5}", UIPanelBuilder.Frequency.Periodic);
      section.AddSlider(() => _SpeedLimit, () => $"{_SpeedLimit * 5}", o => _SpeedLimit = o, 0, 9, true, o => SegmentManager.UpdateSpeedLimit((int)o * 5));
      section.AddField("Group ID", builder.AddInputField(segment.groupId ?? "", (group) => SegmentManager.UpdateGroup(group), "groupId")!);
      section.AddField("Track style", builder.AddEnumDropdown(segment.style, (style) => SegmentManager.UpdateStyle(style)));
      section.AddField("Track class", builder.AddEnumDropdown(segment.trackClass, (trackClass) => SegmentManager.UpdateTrackClass(trackClass)));
    });
    builder.Spacer();
    builder.AddSection("Position", BuildPositionEditor);
    builder.AddSection("Scaling", Utility.BuildScalingEditor);
    builder.Spacer();
    builder.HStack(stack => {
      stack.AddButtonCompact("Inject node", () => NodeManager.InjectNode());
      stack.AddButtonCompact("Flip segment", () => SegmentManager.FlipSegment());
      stack.AddButtonCompact("Remove segment", () => SegmentManager.RemoveSegment());
    });
    builder.HStack(stack => {
      stack.AddButtonCompact(segment.a.id, () => EditorContext.SelectedNode = segment.a);
      stack.AddButtonCompact(segment.b.id, () => EditorContext.SelectedNode = segment.b);
    });
  }

  private static void BuildPositionEditor(UIPanelBuilder builder)
  {
    builder.HStack(stack => {
      stack.AddButtonCompact(() => $"- {EditorContext.Scaling.Movement / 100f:F2}", () => SegmentManager.Move(Direction.down));
      stack.AddButtonCompact(() => $"+ {EditorContext.Scaling.Movement / 100f:F2}", () => SegmentManager.Move(Direction.up));
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
