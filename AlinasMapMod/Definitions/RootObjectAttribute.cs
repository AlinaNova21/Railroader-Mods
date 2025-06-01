using System;

namespace AlinasMapMod.Definitions;

internal class RootObjectAttribute : Attribute
{
  public string Key { get; }

  public RootObjectAttribute(string key)
  {
    Key = key;
  }
}
