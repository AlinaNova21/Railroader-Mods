using System;

namespace MapEditor
{
  class Settings
  {
    public bool Enabled { get; set; } = false;
    public bool ShowHelpers { get; set; } = false;
    [Obsolete("Use ShowHelpers instead")]
    public bool ShowNodeHelpers { get => ShowHelpers; set => ShowHelpers = value; }
  }
}