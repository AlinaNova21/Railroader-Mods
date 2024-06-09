using MapEditor.Extensions;
using MapEditor.Managers;
using Track;
using UI.Builder;
using UI.Common;

namespace MapEditor.Tools
{
  public class UISegmentTool : BaseTool
  {

    protected override string ToolIconPath => "Icons/MoveTool";
    protected override string ToolName => "Segment";
    protected override string ToolDescription => "Segment configuration";

    public Window Window { get; }

    public UISegmentTool()
    {
      Window = EditorMod.Shared.UIHelper.CreateWindow(400, 300, Window.Position.CenterRight);
      EditorMod.Shared.UIHelper.PopulateWindow(Window, BuildWindow);
      Window.OnShownDidChange += shown =>
      {
        if (!shown && Context.ActiveTool == this)
        {
          Deactivate();
        }
      };
    }

    private float? speedLimit;

    private void BuildWindow(UIPanelBuilder builder)
    {
      EditorContext.SegmentSelectedChanged += o => builder.Rebuild();

      var segment = EditorContext.Instance?.SelectedSegment;
      builder.AddField("Priority", builder.AddInputFieldValidated($"{segment?.priority}", value => SegmentManager.UpdatePriority(int.Parse(value)), "\\d+")!);
      builder.AddField("Speed Limit", () => $"{speedLimit ?? segment?.speedLimit ?? 0}", UIPanelBuilder.Frequency.Periodic);
      builder.AddSlider(() => speedLimit ?? segment?.speedLimit ?? 0, () => $"{segment?.speedLimit}", o => speedLimit = o, 0, 45, true, o => SegmentManager.UpdateSpeedLimit((int)o));
      builder.AddField("Group ID", builder.AddInputField(segment?.groupId ?? "", SegmentManager.UpdateGroup, "groupId")!);
      builder.AddField("Track style", builder.AddTrackStylesDropdown(segment?.style ?? TrackSegment.Style.Standard, SegmentManager.UpdateStyle));
      builder.AddField("Track class", builder.AddTrackClassDropdown(segment?.trackClass ?? TrackClass.Mainline, SegmentManager.UpdateTrackClass));
    }

    private void SegmentSelectedChanged(TrackSegment? node)
    {
      if (node == null)
      {
        Window.CloseWindow();
        return;
      }

      Window.Title = $"Segment Editor - {node.id}";
      if (Window.IsShown == false)
      {
        Window.ShowWindow();
      }
    }

    public override void OnActivated()
    {
      SegmentSelectedChanged(Context.SelectedSegment);
      EditorContext.SegmentSelectedChanged += SegmentSelectedChanged;
    }

    protected override void OnDeactivating()
    {
      Window.CloseWindow();
      // NOTE: im unable to reopen window if this is not commented out
      //EditorContext.NodeSelectedChanged -= SelectedNodeChanged;
    }

  }
}
