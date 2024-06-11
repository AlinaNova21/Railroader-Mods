using MapEditor.Extensions;
using MapEditor.Managers;
using Track;
using TriangleNet.Geometry;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs
{
  public class TrackSegmentDialog : DialogBase
  {

    public TrackSegmentDialog() : base("Segment editor", 400, 300, Window.Position.CenterRight)
    {
    }

    private UIPanelBuilder _builder;
    private float? _SpeedLimit;

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      _builder = builder;
      _SpeedLimit = EditorContext.SelectedSegment?.speedLimit / 9 ?? 0;
      builder.AddField("Priority", builder.AddInputFieldValidated($"{EditorContext.SelectedSegment?.priority}", value => SegmentManager.UpdatePriority(int.Parse(value)), "\\d+")!);
      builder.AddField("Speed Limit", () => $"{_SpeedLimit * 5}", UIPanelBuilder.Frequency.Periodic);
      builder.AddSlider(() => _SpeedLimit ?? EditorContext.SelectedSegment?.speedLimit ?? 0, () => $"{_SpeedLimit * 5}", o => _SpeedLimit = o, 0, 9, true, o => SegmentManager.UpdateSpeedLimit((int)o * 5));
      builder.AddField("Group ID", builder.AddInputField(EditorContext.SelectedSegment?.groupId ?? "", SegmentManager.UpdateGroup, "groupId")!);
      builder.AddField("Track style", builder.AddEnumDropdown(EditorContext.SelectedSegment?.style ?? TrackSegment.Style.Standard, SegmentManager.UpdateStyle));
      builder.AddField("Track class", builder.AddEnumDropdown(EditorContext.SelectedSegment?.trackClass ?? TrackClass.Mainline, SegmentManager.UpdateTrackClass));
      builder.AddField("Nodes", builder.HStack(stack =>
      {
        stack.AddButtonCompact(EditorContext.SelectedSegment?.a?.id ?? "", () => EditorContext.SelectedNode = EditorContext.SelectedSegment!.a);
        stack.AddButtonCompact(EditorContext.SelectedSegment?.b?.id ?? "", () => EditorContext.SelectedNode = EditorContext.SelectedSegment!.b);
      })!);
    }

    protected override void AfterWindowClosed()
    {
      base.AfterWindowClosed();
      EditorContext.SelectedSegmentChanged -= SelectedSegmentChanged;
      EditorContext.SelectedSegment = null;
    }

    public void Activate()
    {
      EditorContext.SelectedSegmentChanged += SelectedSegmentChanged;
    }

    public void Deactivate()
    {
      CloseWindow();
    }

    private void SelectedSegmentChanged(TrackSegment? trackSegment)
    {
      if (trackSegment == null)
      {
        CloseWindow();
        return;
      }

      Title = $"Segment Editor - {trackSegment.id}";
      ShowWindow();

      _builder.Rebuild();
    }

  }
}
