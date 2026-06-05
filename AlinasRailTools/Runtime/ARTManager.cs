using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AlinasRailTools.Resources;
using AlinasRailTools.Scripting;
using AlinasRailTools.StrangeCustomsCompat;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using UnityEngine;

namespace AlinasRailTools.Runtime;

/// <summary>
/// MonoBehaviour component that manages AlinasRailTools runtime functionality
/// </summary>
public class ARTManager : MonoBehaviour, IPropertyAccessControlDelegate
{

  internal KeyValueObject _keyValueObject;
    internal Dictionary<string, Value> kvCache = [];

  /// <summary>
  /// Singleton instance of ARTManager
  /// </summary>
  public static ARTManager Shared { get; private set; }

  /// <summary>
  /// KeyValueObject for network-synced state
  /// </summary>
  public KeyValueObject KeyValueObject => _keyValueObject;

  /// <summary>
  /// Virtual filesystem for network-synced file storage
  /// </summary>
  public VirtualFileSystem VFS { get; private set; }

  /// <summary>
  /// Tile manager for custom map tiles
  /// </summary>
  public TileManager TileManager { get; private set; }

  /// <summary>
  /// Custom map manager for map definitions
  /// </summary>
  public CustomMapManager CustomMapManager { get; private set; }

  /// <summary>
  /// Manager registry for resource managers
  /// </summary>
  public ManagerRegistry ManagerRegistry { get; private set; }

  /// <summary>
  /// Graph manager for track operations
  /// </summary>
  public AlinasRailTools.Managers.GraphManager GraphManager { get; private set; }

  /// <summary>
  /// Ops manager for areas, industries, and components
  /// </summary>
  public AlinasRailTools.Managers.OpsManager OpsManager { get; private set; }

  /// <summary>
  /// Constructor - sets the singleton instance
  /// </summary>
  public ARTManager()
  {
    Shared = this;
  }

  /// <summary>
  /// Called when the component is first initialized
  /// </summary>
  public void Awake()
  {
    Debug.Log("ARTManager: Awake called");
    this._keyValueObject = this.GetComponent<KeyValueObject>() ?? this.gameObject.AddComponent<KeyValueObject>();
    StateManager.Shared.RegisterPropertyObject("_art", this._keyValueObject, this);

    // Initialize virtual filesystem
    VFS = new VirtualFileSystem(this._keyValueObject);

    // Initialize manager registry
    ManagerRegistry = new ManagerRegistry();

    // Add managers
    TileManager = gameObject.AddComponent<TileManager>();
    CustomMapManager = gameObject.AddComponent<CustomMapManager>();

    // Discover and register all resource managers
    ManagerDiscovery.DiscoverAndRegister(ManagerRegistry);

    Debug.Log($"ARTManager: Initialized with {ManagerRegistry} resource managers");
  }

  /// <summary>
  /// Initialize managers that need to attach to game objects (called after StateManager creates Ops/Graph)
  /// </summary>
  public void InitializeGameObjectManagers()
  {
    // Attach GraphManager to Graph.Shared
    if (Track.Graph.Shared != null)
    {
      var graphObject = Track.Graph.Shared.gameObject;
      GraphManager = graphObject.GetComponent<AlinasRailTools.Managers.GraphManager>()
                     ?? graphObject.AddComponent<AlinasRailTools.Managers.GraphManager>();
      Debug.Log("ARTManager: Attached GraphManager to Graph.Shared");
    }
    else
    {
      Debug.LogWarning("ARTManager: Graph.Shared is null");
    }

    // Attach OpsManager to Ops (using GameObject.Find)
    var opsObject = GameObject.Find("Ops");
    if (opsObject != null)
    {
      OpsManager = opsObject.GetComponent<AlinasRailTools.Managers.OpsManager>()
                   ?? opsObject.AddComponent<AlinasRailTools.Managers.OpsManager>();
      Debug.Log("ARTManager: Attached OpsManager to Ops object");
    }
    else
    {
      Debug.LogWarning("ARTManager: Could not find Ops object");
    }
  }

  /// <summary>
  /// Called when the component is enabled
  /// </summary>
  public void OnEnable()
  {
    Debug.Log("ARTManager: OnEnable called");
    Messenger.Default.Register<MapWillLoadEvent>(this, OnMapWillLoad);
    Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
  }

  private void OnMapDidLoad(MapDidLoadEvent @event)
  {

  }

  /// <summary>
  /// Load all mods and create merged StateFile (Host only)
  /// The resulting StateFile JSON is stored in KeyValue for network sync
  /// </summary>
  public void LoadAndMergeMods()
  {
    try
    {
      Debug.Log("ARTManager: Loading and merging mods (Host)");

      // Get game directory (assuming we're in the game's root folder)
      var gameDirectory = Directory.GetCurrentDirectory();
      Debug.Log($"ARTManager: Game directory: {gameDirectory}");

      // Create mod loader and register virtual mods
      var modLoader = new AlinasRailTools.Shared.ModLoading.ModLoader(gameDirectory);
      modLoader.RegisterStandardVirtualMods();
      Debug.Log("ARTManager: Registered virtual mods for Railloader, StrangeCustoms and AlinasMapMod");

      // Discover all mods in load order
      var modLoadResult = modLoader.DiscoverModsInLoadOrder();
      if (modLoadResult.HasErrors)
      {
        Debug.LogWarning($"ARTManager: Found {modLoadResult.Errors.Count} mod loading errors:");
        foreach (var error in modLoadResult.Errors)
        {
          Debug.LogWarning($"  - {error.Message}");
        }
      }

      Debug.Log($"ARTManager: Found {modLoadResult.SortedMods.Count} mods to load in order");
      foreach (var mod in modLoadResult.SortedMods)
      {
        Debug.Log($"  - {mod.Definition.Id} ({mod.Definition.Name})");
      }

      // Create merger and converter
      var merger = new AlinasRailTools.Shared.Resources.StateMerger();
      var converter = new AlinasRailTools.Shared.Resources.LegacyConverter();

      // Start with initial world state from managers
      Debug.Log("ARTManager: Capturing initial world state from managers");
      var finalState = new AlinasRailTools.Shared.Resources.StateFile();

      // Reload caches to discover existing game objects
      if (GraphManager != null)
      {
        GraphManager.ReloadCaches();
        Debug.Log("ARTManager: Reloaded Graph caches");
      }

      if (OpsManager != null)
      {
        OpsManager.ReloadCaches();
        Debug.Log("ARTManager: Reloaded Ops caches");
      }

      // Snapshot GraphManager state
      if (GraphManager != null)
      {
        GraphManager.SnapshotToState(finalState);
        Debug.Log("ARTManager: Captured Graph state");
      }

      // Snapshot OpsManager state
      if (OpsManager != null)
      {
        // TODO: OpsManager.SnapshotToState when implemented
        Debug.Log("ARTManager: OpsManager snapshot not yet implemented");
      }

      // Snapshot all resource managers
      var managersState = ManagerRegistry.SnapshotAll();
      finalState = merger.Overlay(finalState, managersState);
      Debug.Log("ARTManager: Captured state from resource managers");

      // Save initial state snapshot for debugging
      var initialStateJson = finalState.ToJson();
      var initialStatePath = Path.Combine(gameDirectory, "ART_initial_state.json");
      File.WriteAllText(initialStatePath, initialStateJson);
      Debug.Log($"ARTManager: Saved initial state snapshot to {initialStatePath} ({initialStateJson.Length} bytes)");

      // Load and merge each mod's game-graph.json
      foreach (var mod in modLoadResult.SortedMods)
      {
        // Skip virtual mods (no physical files)
        if (mod is AlinasRailTools.Shared.ModLoading.VirtualModInfo virtualMod && virtualMod.IsVirtual)
        {
          Debug.Log($"ARTManager: Skipping virtual mod {mod.Definition.Id}");
          continue;
        }

        // Look for game-graph.json in mod directory
        var gameGraphPath = Path.Combine(mod.Directory, "game-graph.json");
        if (!File.Exists(gameGraphPath))
        {
          Debug.Log($"ARTManager: No game-graph.json found for {mod.Definition.Id}, skipping");
          continue;
        }

        Debug.Log($"ARTManager: Loading {mod.Definition.Id} from {gameGraphPath}");

        // Load and convert to StateFile
        var gameGraphJson = File.ReadAllText(gameGraphPath);
        var legacyJson = Newtonsoft.Json.Linq.JObject.Parse(gameGraphJson);
        var modState = converter.ConvertToStateFile(legacyJson);

        // Merge into final state
        finalState = merger.Overlay(finalState, modState);
        Debug.Log($"ARTManager: Merged {mod.Definition.Id} state");
      }

      // Serialize final state to JSON
      var finalStateJson = finalState.ToJson();
      Debug.Log($"ARTManager: Final state JSON size: {finalStateJson.Length} bytes");

      // Save final merged state for debugging
      var finalStatePath = Path.Combine(gameDirectory, "ART_final_state.json");
      File.WriteAllText(finalStatePath, finalStateJson);
      Debug.Log($"ARTManager: Saved final merged state to {finalStatePath}");

      // Store in KeyValue for network sync
      _keyValueObject.Set("stateFile", Value.String(finalStateJson));

      Debug.Log("ARTManager: Successfully loaded and merged mods");
    }
    catch (Exception ex)
    {
      Debug.LogError($"ARTManager: Error loading and merging mods: {ex.Message}");
      Debug.LogError($"Stack trace: {ex.StackTrace}");
    }
  }

  /// <summary>
  /// Load StateFile from KeyValue and apply through ManagerRegistry
  /// Called on both host and clients after state sync
  /// </summary>
  public void ApplyStateFromKeyValue()
  {
    try
    {
      Debug.Log("ARTManager: Applying state from KeyValue");

      // Get the merged StateFile JSON from KeyValue
      Value stateValue;
      if (!_keyValueObject.Dictionary.TryGetValue("stateFile", out stateValue))
      {
        Debug.LogWarning("ARTManager: No stateFile found in KeyValue");
        return;
      }
      var stateJson = stateValue.StringValue;
      if (string.IsNullOrEmpty(stateJson))
      {
        Debug.LogWarning("ARTManager: No stateFile found in KeyValue");
        return;
      }

      Debug.Log($"ARTManager: Retrieved state JSON from KeyValue ({stateJson.Length} bytes)");

      // Deserialize and apply through ManagerRegistry
      var stateFile = AlinasRailTools.Shared.Resources.StateFile.FromJson(stateJson);
      Debug.Log($"ARTManager: Deserialized StateFile");

      ManagerRegistry.ApplyToAll(stateFile);

      Debug.Log("ARTManager: Successfully applied state from KeyValue");
    }
    catch (Exception ex)
    {
      Debug.LogError($"ARTManager: Error applying state from KeyValue: {ex.Message}");
      Debug.LogError($"Stack trace: {ex.StackTrace}");
    }
  }

  private void OnMapWillLoad(MapWillLoadEvent @event)
  {
    // Mod loading now happens in StateManagerPopulatePatch after managers are initialized
  }

  public void OnDisable()
  {
    Messenger.Default.Unregister(this);
    StateManager.Shared.UnregisterPropertyObject("_art");

  }
  public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key) => AuthorizationRequirement.HostOnly;
}
