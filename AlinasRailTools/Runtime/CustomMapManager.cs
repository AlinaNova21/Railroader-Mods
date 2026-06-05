using System.Collections.Generic;
using AlinasRailTools.Map;
using Character;
using Game.Progression;
using KeyValue.Runtime;
using Map.Runtime;
using Serilog;
using UnityEngine;

namespace AlinasRailTools.Runtime;

public class CustomMapManager : MonoBehaviour
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<CustomMapManager>();

    public static CustomMapManager Instance { get; private set; }

    private KeyValueObject _keyValueObject;

    public Dictionary<string, MapDefinition> MapDefinitions { get; } = new();
    public MapDefinition? Selected { get; set; }

    public void Awake()
    {
        Instance = this;
        _keyValueObject = ARTManager.Shared.GetComponent<KeyValueObject>();

        // Create a test map definition
        MapDefinition def = new()
        {
            Identifier = "MemphisTN",
            Name = "Custom Map - Custom Start",
            MapName = "MemphisTN",
            Description = "This is a custom map.",
            ProgressionId = "test",
            InitialMoney = 1234,
            SpawnPosition = new Vector3(0, 0, 0),
            SpawnRotation = new Vector3(0, 0, 0),
            CarPlacements = new List<SetupDescriptor.CarPlacement>(),
            ShowTutorial = false
        };

        MapDefinitions.Add(def.Identifier, def);
    }

    public void Sync()
    {
        foreach (var def in MapDefinitions.Values)
        {
            var spawn = new GameObject($"{def.Identifier} Spawn Point");
            spawn.transform.parent = transform;
            spawn.transform.position = def.SpawnPosition;
            spawn.transform.eulerAngles = def.SpawnRotation;
            var sp = spawn.AddComponent<SpawnPoint>();

            var setup = new GameObject($"{def.Identifier} Setup Descriptor");
            setup.transform.parent = transform;
            var desc = setup.AddComponent<SetupDescriptor>();
            desc.identifier = def.Identifier;
            desc.showTutorial = def.ShowTutorial;
            desc.initialMoney = def.InitialMoney;
            desc.placements = def.CarPlacements.ToArray();
            desc.spawnPoint = sp;
        }
    }

    internal void InitMap()
    {
        var def = Selected;
        var map = def?.MapName ?? "BushnellWhittier";
        Logger.Debug("Init Map: {MapName}", map);
        var mm = GameObject.FindObjectOfType<MapManager>(true);
        mm.directoryName = map;
        if (map == "BushnellWhittier") return; // Stock map, leave it alone

        var ops = GameObject.Find("Ops");
        //DestroyAllChildren(ops);
        var scenery = GameObject.Find("Large Scenery");
        //DestroyAllChildren(scenery);
    }

    internal void DestroyAllChildren(GameObject parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.transform.GetChild(i).gameObject);
        }
    }

    public void SaveSelectedMapToKVO()
    {
        // TODO: Temporarily disabled - Vector3 and other Unity types need custom serialization
        // if (StateManager.IsHost && Selected != null)
        // {
        //     var json = JsonConvert.SerializeObject(Selected);
        //     _keyValueObject["map"] = Value.String(json);
        //     Logger.Information("Saved map definition to KVO: {MapName}", Selected.Value.Name);
        // }
        Logger.Debug("SaveSelectedMapToKVO stubbed - needs Vector3 serialization support");
    }

    public void LoadMapFromKVO()
    {
        // TODO: Temporarily disabled - Vector3 and other Unity types need custom serialization
        // try
        // {
        //     var mapValue = _keyValueObject["map"];
        //     if (mapValue != null && !string.IsNullOrEmpty(mapValue.StringValue))
        //     {
        //         var json = mapValue.StringValue;
        //         var mapDef = JsonConvert.DeserializeObject<MapDefinition>(json);
        //         Logger.Information("Loaded map definition from KVO: {MapName}", mapDef.Name);
        //         ApplyMapDefinition(mapDef);
        //     }
        // }
        // catch (Exception ex)
        // {
        //     Logger.Error(ex, "Failed to load map definition from KVO");
        // }
        Logger.Debug("LoadMapFromKVO stubbed - needs Vector3 serialization support");
    }

    // Stub for future implementation
    private void ApplyMapDefinition(MapDefinition mapDef)
    {
        // TODO: Create spawn points, setup descriptors, etc.
        // This is complex and will be implemented later
        Logger.Warning("ApplyMapDefinition not yet implemented");
    }
}
