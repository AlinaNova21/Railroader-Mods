using System.Collections.Generic;

namespace AlinasRailTools.Definitions;

public class Manifest
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "0.0.0";
    public List<string> Authors { get; set; } = [];
    public string Description { get; set; } = "No description available.";
    public string License { get; set; } = "MIT";
    public string Repository { get; set; } = "";
    public Dictionary<string, string> Dependencies { get; set; } = [];
    public string AssetsPath { get; set; } = "Assets";
    public Dictionary<string, string> Prefabs { get; set; } = [];
    public List<Map> Maps { get; set; } = [];
}