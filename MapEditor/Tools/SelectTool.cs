namespace MapEditor.Tools
{
    public class SelectTool : BaseTool
    {
        protected override string ToolIconPath => "Icons/SelectTool";
        protected override string ToolName => "Select";
        protected override string ToolDescription => "Select objects";

        public override void OnActivated()
        {
        }

        protected override void OnDeactivating()
        {
        }
    }
  
}