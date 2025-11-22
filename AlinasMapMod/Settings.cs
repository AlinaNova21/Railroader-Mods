namespace AlinasMapMod;

class Settings
{
  public bool EnableDeliveries { get; set; } = true;
  public bool FreeMilestones { get; set; } = false;
  public float DeliveryCarCountMultiplier { get; set; } = 1;
  public string ProgressionsDumpPath { get; set; } = "";
  public bool DownloadMissingTiles { get; set; } = true;
}
