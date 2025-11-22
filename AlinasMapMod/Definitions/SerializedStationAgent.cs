using AlinasMapMod.Caches;
using AlinasMapMod.Stations;
using AlinasMapMod.Validation;
using UnityEngine;

namespace AlinasMapMod.Definitions;
public class SerializedStationAgent : SerializedComponentBase<PaxStationAgent>,
 ICreatableComponent<PaxStationAgent>,
 IDestroyableComponent<PaxStationAgent>
{
  public SerializedVector3 Position { get; set; }
  public SerializedVector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string PassengerStop { get; set; } = "whittier";

  protected override void ConfigureValidation()
  {
    RuleFor(() => Prefab)
      .Required()
      .AsGameObjectUri(VanillaPrefabs.AvailableStationPrefabs);

    RuleFor(() => PassengerStop)
      .Required();

    RuleFor(() => Position)
      .Required();

    RuleFor(() => Rotation)
      .Required();
  }

  public override PaxStationAgent Create(string id)
  {
    GameObject go = null;
    try
    {
      go = new GameObject(id);
      go.transform.parent = Utils.GetParent("StationAgents").transform;
      var comp = go.AddComponent<PaxStationAgent>();
      comp.name = id;
      comp.identifier = id;
      Write(comp);
      StationAgentCache.Instance[id] = comp;
      return comp;
    }
    catch
    {
      // Clean up the GameObject if creation failed
      if (go != null)
      {
        UnityEngine.Object.DestroyImmediate(go);
      }
      // Remove from cache if it was added
      StationAgentCache.Instance.Remove(id);
      throw;
    }
  }

  public override void Write(PaxStationAgent comp)
  {
    comp.transform.localPosition = Position;
    comp.transform.localEulerAngles = Rotation;
    comp.Prefab = Prefab;
    comp.PassengerStop = PassengerStop;
  }

  public override void Read(PaxStationAgent comp)
  {
    Position = comp.transform.localPosition;
    Rotation = comp.transform.localEulerAngles;
    Prefab = comp.Prefab;
    PassengerStop = comp.PassengerStop;
  }

  public void Destroy(PaxStationAgent comp)
  {
    GameObject.Destroy(comp.gameObject);
    StationAgentCache.Instance.Remove(comp.identifier);
  }
}
