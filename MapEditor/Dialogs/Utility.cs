using MapEditor.Managers;
using UI.Builder;

namespace MapEditor.Dialogs
{
  public static class Utility
  {

    public static void BuildScalingEditor(UIPanelBuilder builder)
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


  }
}
