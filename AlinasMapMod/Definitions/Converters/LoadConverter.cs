using System;
using System.Linq;
using Model.Ops.Definition;
using Newtonsoft.Json;
using UnityEngine;

namespace AlinasMapMod.Definitions.Converters;

public class LoadConverter : JsonConverter<Load>
{
  public override void WriteJson(JsonWriter writer, Load value, JsonSerializer serializer)
  {
    writer.WriteValue(value.id);
  }

  public override Load ReadJson(JsonReader reader, Type objectType, Load existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    string s = (string)reader.Value;
    if (existingValue?.id == s) return existingValue;
    var load = Resources.FindObjectsOfTypeAll<Load>().SingleOrDefault(l => l.id == s);
    if (load == null || load == default) {
      throw new Exception($"Load with id {s} not found");
    }
    return load;
  }
}
