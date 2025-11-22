using System;

namespace AlinasUtils;

public class Settings
{
  public string SaveToLoadOnStartup { get; set; } = "";
  public bool AutoLoadSaveOnStartup { get; set; } = false;
  public bool DisableDerailing { get; set; } = false;
  [Obsolete("Combined into DisableDamage")]
  public bool DisableDamageOnCurves { get => false; }
  [Obsolete("Combined into DisableDamage")]
  public bool DisableCollisions { get => false; }
  public bool DisableDamage { get; set; } = false;
  public int MaxCameraDistance { get; set; } = 500;
  public int MaxTileLoadDistance { get; set; } = 1500;
}
