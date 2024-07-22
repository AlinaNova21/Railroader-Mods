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
        stack.AddButtonCompact(() => "0.01", () => NodeManager.Scaling = 0.01f);
        stack.AddButtonCompact(() => "0.1", () => NodeManager.Scaling = 0.1f);
        stack.AddButtonCompact(() => "0.5", () => NodeManager.Scaling = 0.5f);
        stack.AddButtonCompact(() => "1", () => NodeManager.Scaling = 1f);
        stack.AddButtonCompact(() => "5", () => NodeManager.Scaling = 5f);
        stack.AddButtonCompact(() => "10", () => NodeManager.Scaling = 10f);
        stack.AddButtonCompact(() => "Car", () => NodeManager.Scaling = 12.2f);
      });
    }

  }
}
