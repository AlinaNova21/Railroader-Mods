using AlinasMapMod.Caches;
using AlinasMapMod.Stations;
using UnityEngine;

namespace AlinasMapMod.Definitions;
public class SerializedStationAgent :
 ISerializedPatchableComponent<PaxStationAgent>,
 ICreatableComponent<PaxStationAgent>,
 IDestroyableComponent<PaxStationAgent>
{
  public SerializedVector3 Position { get; set; }
  public SerializedVector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string PassengerStop { get; set; } = "whittier";

  public PaxStationAgent Create(string id)
  {
    var go = new GameObject(id);
    go.transform.parent = Utils.GetParent("StationAgents").transform;
    var comp = go.AddComponent<PaxStationAgent>();
    comp.name = id;
    comp.identifier = id;
    Write(comp);
    StationAgentCache.Instance[id] = comp;
    return comp;
  }

  public void Write(PaxStationAgent comp)
  {
    comp.transform.localPosition = Position;
    comp.transform.localEulerAngles = Rotation;
    comp.Prefab = Prefab;
    comp.PassengerStop = PassengerStop;
  }

  public void Read(PaxStationAgent comp)
  {
    Position = comp.transform.localPosition;
    Rotation = comp.transform.localEulerAngles;
    Prefab = comp.Prefab;
    PassengerStop = comp.PassengerStop;
  }

  public void Validate()
  {
    if (string.IsNullOrEmpty(Prefab))
      throw new ValidationException("Prefab must be set.");
    Utils.ValidatePrefab(Prefab, VanillaPrefabs.AvailableStationPrefabs);
    if (string.IsNullOrEmpty(PassengerStop))
      throw new ValidationException("PassengerStop must be set.");
    if (Position == null || Rotation == null)
      throw new ValidationException("Position and Rotation must be defined.");
  }

  public void Destroy(PaxStationAgent comp)
  {
    GameObject.Destroy(comp.gameObject);
    StationAgentCache.Instance.Remove(comp.identifier);
  }
}
