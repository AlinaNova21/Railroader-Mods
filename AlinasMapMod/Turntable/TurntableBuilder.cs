using AlinasMapMod.Definitions;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Turntable;

public class TurntableBuilder : ISplineyBuilder
{
  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var tt = data.ToObject<SerializedTurntable>();
    var pos = new Vector3(tt.Position.x, tt.Position.y, tt.Position.z);
    var rot = new Vector3(tt.Rotation.x, tt.Rotation.y, tt.Rotation.z);
    var ttg = TurntableGenerator.Generate(id, tt.RoundhouseStalls, pos, rot);
    ttg.transform.parent = parentTransform;
    return ttg;
  }
}

