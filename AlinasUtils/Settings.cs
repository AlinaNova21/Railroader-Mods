using System;

namespace AlinasUtils;

internal class Settings
{
  public string SaveToLoadOnStartup { get; set; } = "";
  public bool AutoLoadSaveOnStartup { get; set; } = false;
  public bool DisableDerailing { get; set; } = false;
  [Obsolete("Combined into DisableDamage")]
  public bool DisableDamageOnCurves { get => false; }
  [Obsolete("Combined into DisableDamage")]
  public bool DisableCollisions { get => false; }
  public bool DisableDamage { get; set; } = false;
}
