using System.Collections.Generic;

namespace AlinasRailTools.Definitions;

public class Patches
{
    public string Id { get; set; } = "";
    public string File { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Dependencies { get; set; } = [];
}