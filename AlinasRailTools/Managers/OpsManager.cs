using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Extensions;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Managers;
using Model;
using Model.Ops;
using Model.Ops.Definition;
using UnityEngine;

namespace AlinasRailTools.Managers;

/// <summary>
/// Manager for operations objects (areas, industries, components)
/// Handles hierarchical structure: Area > Industry > IndustryComponent/PassengerStop
/// </summary>
public class OpsManager : MonoBehaviour
{
  public AreaManager Areas { get; private set; }

  // Shared caches for hierarchical lookups
  private Dictionary<string, Area> AllAreas = new();
  private Dictionary<string, Industry> AllIndustries = new();
  private Dictionary<string, IndustryComponent> AllComponents = new();
  private Dictionary<string, PassengerStop> AllPassengerStops = new();

  void Awake()
  {
    Areas = new AreaManager(this);
  }

  /// <summary>
  /// Reloads all ops object caches from the scene
  /// </summary>
  public void ReloadCaches()
  {
    AllAreas.Clear();
    AllIndustries.Clear();
    AllComponents.Clear();
    AllPassengerStops.Clear();

    var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (var area in areas)
    {
      AllAreas[area.identifier] = area;

      // Find industries under this area
      var industries = area.GetComponentsInChildren<Industry>(true);
      foreach (var industry in industries)
      {
        var industryKey = $"{area.identifier}/{industry.name}";
        AllIndustries[industryKey] = industry;

        // Find components under this industry
        var components = industry.GetComponentsInChildren<IndustryComponent>(true);
        foreach (var component in components)
        {
          var componentKey = $"{area.identifier}/{industry.name}/{component.subIdentifier}";
          AllComponents[componentKey] = component;
        }

        // Find passenger stops under this industry
        var stops = industry.GetComponentsInChildren<PassengerStop>(true);
        foreach (var stop in stops)
        {
          var stopKey = $"{area.identifier}/{industry.name}/{stop.identifier}";
          AllPassengerStops[stopKey] = stop;
        }
      }
    }
  }

  /// <summary>
  /// Helper to convert string load identifier to Load object
  /// </summary>
  private static Load GetLoad(string loadIdentifier)
  {
    if (string.IsNullOrEmpty(loadIdentifier))
      return null;

    return TrainController.Shared?.carPrototypeLibrary?.opsLoads?
      .FirstOrDefault(l => l.name == loadIdentifier);
  }

  /// <summary>
  /// Helper to convert Load object to string identifier
  /// </summary>
  private static string GetLoadIdentifier(Load load)
  {
    return load?.name ?? "";
  }

  #region Area Wrapper Methods

  public Area Apply(string id, SerializedArea data) => Areas.Apply(id, data);
  public void Remove(string areaId) => Areas.Remove(areaId);
  public Area Get(string areaId) => Areas.Get(areaId);
  public SerializedArea Serialize(string areaId) => Areas.Serialize(areaId);
  public List<Area> ListAreas() => Areas.List();
  public bool AreaExists(string areaId) => Areas.Exists(areaId);

  #endregion

  #region Industry Wrapper Methods

  public Industry Apply(string areaId, string industryId, SerializedIndustry data) =>
    Areas.Industries.Apply($"{areaId}/{industryId}", data);

  public void Remove(string areaId, string industryId) =>
    Areas.Industries.Remove($"{areaId}/{industryId}");

  public Industry Get(string areaId, string industryId) =>
    Areas.Industries.Get($"{areaId}/{industryId}");

  public SerializedIndustry Serialize(string areaId, string industryId) =>
    Areas.Industries.Serialize($"{areaId}/{industryId}");

  public List<Industry> ListIndustries(string areaId) =>
    Areas.Industries.List();

  public bool IndustryExists(string areaId, string industryId) =>
    Areas.Industries.Exists($"{areaId}/{industryId}");

  #endregion

  #region Component Wrapper Methods

  public MonoBehaviour Apply(string areaId, string industryId, string componentId, SerializedIndustryComponent data) =>
    Areas.Industries.Components.Apply($"{areaId}/{industryId}/{componentId}", data);

  public void Remove(string areaId, string industryId, string componentId) =>
    Areas.Industries.Components.Remove($"{areaId}/{industryId}/{componentId}");

  public MonoBehaviour Get(string areaId, string industryId, string componentId) =>
    Areas.Industries.Components.Get($"{areaId}/{industryId}/{componentId}");

  public SerializedIndustryComponent Serialize(string areaId, string industryId, string componentId) =>
    Areas.Industries.Components.Serialize($"{areaId}/{industryId}/{componentId}");

  public List<MonoBehaviour> ListComponents(string areaId, string industryId) =>
    Areas.Industries.Components.List();

  public bool ComponentExists(string areaId, string industryId, string componentId) =>
    Areas.Industries.Components.Exists($"{areaId}/{industryId}/{componentId}");

  #endregion

  #region Bulk Operations

  /// <summary>
  /// Serializes all areas and their hierarchical content
  /// </summary>
  public Dictionary<string, SerializedArea> SerializeAreas()
  {
    var result = new Dictionary<string, SerializedArea>();
    foreach (var kvp in AllAreas)
    {
      result[kvp.Key] = Areas.Serialize(kvp.Key);
    }
    return result;
  }

  /// <summary>
  /// Applies multiple areas from serialized data
  /// </summary>
  public void ApplyAreas(Dictionary<string, SerializedArea> areas)
  {
    foreach (var kvp in areas)
    {
      Areas.Apply(kvp.Key, kvp.Value);
    }
  }

  #endregion

  #region Nested Managers

  /// <summary>
  /// Manages Area entities
  /// </summary>
  public class AreaManager : IEntityManager<Area, SerializedArea>
  {
    public OpsManager Shared { get; }
    private IndustryManager _industries;

    public AreaManager(OpsManager shared)
    {
      Shared = shared;
      _industries = new IndustryManager(shared);
    }

    public IndustryManager Industries => _industries;

    public Area Apply(string id, SerializedArea data)
    {
      if (!Shared.AllAreas.TryGetValue(id, out var area))
      {
        // Create new Area GameObject
        var go = new GameObject($"Area_{id}");
        go.transform.SetParent(Shared.transform);
        area = go.AddComponent<Area>();
        Shared.AllAreas[id] = area;
      }

      // Update area properties
      area.identifier = data.Identifier;
      // GroupIds are typically managed separately through the graph

      return area;
    }

    public void Remove(string id)
    {
      if (Shared.AllAreas.TryGetValue(id, out var area))
      {
        Shared.AllAreas.Remove(id);
        if (area != null)
        {
          Object.Destroy(area.gameObject);
        }
      }
    }

    public Area Get(string id)
    {
      Shared.AllAreas.TryGetValue(id, out var area);
      return area;
    }

    public SerializedArea Serialize(string id)
    {
      var area = Get(id);
      if (area == null)
        return null;

      return new SerializedArea
      {
        Identifier = area.identifier,
        GroupIds = new List<string>() // TODO: Get groups from area when property name is confirmed
      };
    }

    public List<Area> List()
    {
      return Shared.AllAreas.Values.ToList();
    }

    public bool Exists(string id)
    {
      return Shared.AllAreas.ContainsKey(id);
    }
  }

  /// <summary>
  /// Manages Industry entities within Areas
  /// </summary>
  public class IndustryManager : IEntityManager<Industry, SerializedIndustry>
  {
    public OpsManager Shared { get; }
    private ComponentManager _components;

    public IndustryManager(OpsManager shared)
    {
      Shared = shared;
      _components = new ComponentManager(shared);
    }

    public ComponentManager Components => _components;

    public Industry Apply(string id, SerializedIndustry data)
    {
      if (!Shared.AllIndustries.TryGetValue(id, out var industry))
      {
        // Create new Industry GameObject
        var go = new GameObject($"Industry_{id}");
        industry = go.AddComponent<Industry>();
        Shared.AllIndustries[id] = industry;
      }

      // Update industry properties
      industry.name = data.Name;
      industry.transform.localPosition = data.LocalPosition.ToUnity();
      // UsesContract would be set through industry definition system

      // Apply components
      foreach (var kvp in data.Components)
      {
        Components.Apply(kvp.Key, kvp.Value);
      }

      return industry;
    }

    public void Remove(string id)
    {
      if (Shared.AllIndustries.TryGetValue(id, out var industry))
      {
        Shared.AllIndustries.Remove(id);
        if (industry != null)
        {
          Object.Destroy(industry.gameObject);
        }
      }
    }

    public Industry Get(string id)
    {
      Shared.AllIndustries.TryGetValue(id, out var industry);
      return industry;
    }

    public SerializedIndustry Serialize(string id)
    {
      var industry = Get(id);
      if (industry == null)
        return null;

      var components = new Dictionary<string, SerializedIndustryComponent>();
      // Serialize all components for this industry
      foreach (var kvp in Shared.AllComponents.Where(c => c.Value.Industry == industry))
      {
        var serialized = Components.Serialize(kvp.Key);
        if (serialized != null)
        {
          components[kvp.Key] = serialized;
        }
      }

      return new SerializedIndustry
      {
        Name = industry.name,
        LocalPosition = industry.transform.localPosition.ToSerialized(),
        UsesContract = false, // TODO: Get from industry.definition when property name is confirmed
        Components = components
      };
    }

    public List<Industry> List()
    {
      return Shared.AllIndustries.Values.ToList();
    }

    public bool Exists(string id)
    {
      return Shared.AllIndustries.ContainsKey(id);
    }
  }

  /// <summary>
  /// Manages IndustryComponent entities and PassengerStop (both handled as MonoBehaviour)
  /// Note: Does not implement IEntityManager because it handles multiple types
  /// </summary>
  public class ComponentManager
  {
    public OpsManager Shared { get; }

    public ComponentManager(OpsManager shared)
    {
      Shared = shared;
    }

    public MonoBehaviour Apply(string id, SerializedIndustryComponent data)
    {
      // Handle PassengerStop separately
      if (data is SerializedPassengerStop stopData)
      {
        return ApplyPassengerStop(id, stopData);
      }

      // Handle IndustryComponents
      if (!Shared.AllComponents.TryGetValue(id, out var component))
      {
        // Create new component based on type
        var go = new GameObject($"Component_{data.Type}_{id}");
        component = CreateComponentByType(go, data);
        Shared.AllComponents[id] = component;
      }

      // Update component properties based on type
      UpdateComponentProperties(component, data);

      return component;
    }

    private PassengerStop ApplyPassengerStop(string id, SerializedPassengerStop data)
    {
      if (!Shared.AllPassengerStops.TryGetValue(id, out var stop))
      {
        var go = new GameObject($"PassengerStop_{id}");
        stop = go.AddComponent<PassengerStop>();
        Shared.AllPassengerStops[id] = stop;
      }

      // Update passenger stop properties
      stop.identifier = data.Identifier;
      stop.name = data.DisplayName;
      stop.transform.localPosition = data.Position.ToUnity();

      return stop;
    }

    private IndustryComponent CreateComponentByType(GameObject go, SerializedIndustryComponent data)
    {
      // Create appropriate component type based on serialized data type
      return data switch
      {
        SerializedIndustryLoader => go.AddComponent<IndustryLoader>(),
        SerializedInterchangedIndustryLoader => go.AddComponent<InterchangedIndustryLoader>(),
        SerializedIndustryUnloader => go.AddComponent<IndustryUnloader>(),
        SerializedTeamTrack => go.AddComponent<TeamTrack>(),
        _ => go.AddComponent<IndustryComponent>()
      };
    }

    private void UpdateComponentProperties(IndustryComponent component, SerializedIndustryComponent data)
    {
      // Base properties
      component.subIdentifier = data.SubIdentifier;

      // Type-specific properties
      switch (data)
      {
        case SerializedIndustryLoaderBase loaderData when component is IndustryLoaderBase loader:
          loader.load = GetLoad(loaderData.Load);
          loader.productionRate = loaderData.ProductionRate;
          loader.maxStorage = loaderData.MaxStorage;
          loader.orderEmpties = loaderData.OrderEmpties;
          if (loaderData is SerializedInterchangedIndustryLoader interchangedData &&
              component is InterchangedIndustryLoader interchangedLoader)
          {
            // Handle interchanged loader specific properties
            // targetIndustries would be set through separate system
          }
          break;

        case SerializedIndustryUnloader unloaderData when component is IndustryUnloader unloader:
          unloader.load = GetLoad(unloaderData.Load);
          unloader.carUnloadRate = unloaderData.CarUnloadRate;
          unloader.storageConsumptionRate = unloaderData.StorageConsumptionRate;
          unloader.maxStorage = unloaderData.MaxStorage;
          unloader.orderAwayEmpties = unloaderData.OrderAwayEmpties;
          unloader.orderLoads = unloaderData.OrderLoads;
          break;

        case SerializedTeamTrack teamTrackData when component is TeamTrack teamTrack:
          // ProfileName would be used to look up TeamTrackProfile
          break;
      }
    }

    public void Remove(string id)
    {
      // Try removing as IndustryComponent
      if (Shared.AllComponents.TryGetValue(id, out var component))
      {
        Shared.AllComponents.Remove(id);
        if (component != null)
        {
          Object.Destroy(component.gameObject);
        }
        return;
      }

      // Try removing as PassengerStop
      if (Shared.AllPassengerStops.TryGetValue(id, out var stop))
      {
        Shared.AllPassengerStops.Remove(id);
        if (stop != null)
        {
          Object.Destroy(stop.gameObject);
        }
      }
    }

    public MonoBehaviour Get(string id)
    {
      // Try as IndustryComponent first
      if (Shared.AllComponents.TryGetValue(id, out var component))
        return component;

      // Try as PassengerStop
      if (Shared.AllPassengerStops.TryGetValue(id, out var stop))
        return stop;

      return null;
    }

    public SerializedIndustryComponent Serialize(string id)
    {
      var obj = Get(id);
      if (obj == null)
        return null;

      // Handle PassengerStop
      if (obj is PassengerStop stop)
      {
        return new SerializedPassengerStop
        {
          Type = "passenger-stop",
          SubIdentifier = stop.identifier,
          Identifier = stop.identifier,
          DisplayName = stop.name,
          Position = stop.transform.localPosition.ToSerialized()
        };
      }

      // Handle IndustryComponents
      var component = obj as IndustryComponent;
      if (component == null)
        return null;

      // Create appropriate serialized type based on component runtime type
      return component switch
      {
        InterchangedIndustryLoader interchangedLoader => new SerializedInterchangedIndustryLoader
        {
          Type = "interchanged-loader",
          SubIdentifier = interchangedLoader.subIdentifier,
          Load = GetLoadIdentifier(interchangedLoader.load),
          ProductionRate = 0f, // TODO: Get from base class when property is accessible
          MaxStorage = 0f, // TODO: Get from base class when property is accessible
          OrderEmpties = false, // TODO: Get from base class when property is accessible
          TargetIndustries = new string[0] // Would need to serialize from actual targets
        },
        IndustryLoader loader => new SerializedIndustryLoader
        {
          Type = "loader",
          SubIdentifier = loader.subIdentifier,
          Load = GetLoadIdentifier(loader.load),
          ProductionRate = loader.productionRate,
          MaxStorage = loader.maxStorage,
          OrderEmpties = loader.orderEmpties
        },
        IndustryUnloader unloader => new SerializedIndustryUnloader
        {
          Type = "unloader",
          SubIdentifier = unloader.subIdentifier,
          Load = GetLoadIdentifier(unloader.load),
          CarUnloadRate = unloader.carUnloadRate,
          StorageConsumptionRate = unloader.storageConsumptionRate,
          MaxStorage = unloader.maxStorage,
          OrderAwayEmpties = unloader.orderAwayEmpties,
          OrderLoads = unloader.orderLoads
        },
        TeamTrack teamTrack => new SerializedTeamTrack
        {
          Type = "team-track",
          SubIdentifier = teamTrack.subIdentifier,
          ProfileName = "" // Would need to get from profile
        },
        _ => new SerializedIndustryComponent
        {
          Type = "unknown",
          SubIdentifier = component.subIdentifier
        }
      };
    }

    public List<MonoBehaviour> List()
    {
      var list = new List<MonoBehaviour>();
      list.AddRange(Shared.AllComponents.Values);
      list.AddRange(Shared.AllPassengerStops.Values);
      return list;
    }

    public bool Exists(string id)
    {
      return Shared.AllComponents.ContainsKey(id) || Shared.AllPassengerStops.ContainsKey(id);
    }
  }

  #endregion
}
