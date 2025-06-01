using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasMapMod.Definitions;

public abstract class PatchableObject : MonoBehaviour
{
  public string Identifier { get; set; }
  public PatchableObject Parent { get; set; }
  public abstract void Create();
  public abstract void Destroy();
  public abstract void Update(JObject data);
  public abstract JToken ToJson();
}
