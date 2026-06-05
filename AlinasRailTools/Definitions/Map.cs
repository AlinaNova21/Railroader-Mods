using System.Collections.Generic;
using System.Numerics;

namespace AlinasRailTools.Definitions;

public class Map
{
    public string Id { get; set; } = "";
    public string Identifier { get; set; } = "";
    public string MapName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ProgressionId { get; set; } = "";
    public int InitialMoney { get; set; } = 50000;
    public Vector3 SpawnPosition { get; set; } = Vector3.Zero;
    public Vector3 SpawnRotation { get; set; } = Vector3.Zero;
    public bool ShowTutorial { get; set; } = true;
    public Dictionary<string, Patches> Patches { get; set; } = [];
}