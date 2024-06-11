using JetBrains.Annotations;
using MapEditor.Extensions;
using MapEditor.Managers;
using Track;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs
{
  public class TrackSegmentDialog : DialogBase
  {

    public TrackSegmentDialog() : base("Segment editor", 400, 300, Window.Position.CenterRight)
    {
    }

    private UIPanelBuilder? _builder;
    private float? _SpeedLimit;

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      _builder = builder;
      _SpeedLimit = EditorContext.SelectedSegment?.speedLimit / 9 ?? 0;
      builder.AddSection("Properties", section => {
        section.AddField("Priority", builder.AddInputFieldValidated($"{EditorContext.SelectedSegment?.priority}", value => SegmentManager.UpdatePriority(int.Parse(value)), "\\d+")!);
        section.AddField("Speed Limit", () => $"{_SpeedLimit * 5}", UIPanelBuilder.Frequency.Periodic);
        section.AddSlider(() => _SpeedLimit ?? EditorContext.SelectedSegment?.speedLimit ?? 0, () => $"{_SpeedLimit * 5}", o => _SpeedLimit = o, 0, 9, true, o => SegmentManager.UpdateSpeedLimit((int)o * 5));
        section.AddField("Group ID", builder.AddInputField(EditorContext.SelectedSegment?.groupId ?? "", SegmentManager.UpdateGroup, "groupId")!);
        section.AddField("Track style", builder.AddEnumDropdown(EditorContext.SelectedSegment?.style ?? TrackSegment.Style.Standard, SegmentManager.UpdateStyle));
        section.AddField("Track class", builder.AddEnumDropdown(EditorContext.SelectedSegment?.trackClass ?? TrackClass.Mainline, SegmentManager.UpdateTrackClass));
      });
      builder.Spacer();
      builder.AddSection("Position", BuildPositionEditor);
      builder.AddSection("Scaling", Utility.BuildScalingEditor);
      builder.Spacer();
      builder.AddField("Nodes", builder.HStack(stack =>
      {
        stack.AddButtonCompact(EditorContext.SelectedSegment?.a?.id ?? "", () =>
        {
          var node = EditorContext.SelectedSegment!.a;
          EditorContext.SelectedSegment = null;
          EditorContext.SelectedNode = node;
        });
        stack.AddButtonCompact(EditorContext.SelectedSegment?.b?.id ?? "", () =>
        {
          var node = EditorContext.SelectedSegment!.b;
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
      _builder?.Rebuild();
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
