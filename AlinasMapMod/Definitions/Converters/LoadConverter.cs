using System;
using System.Linq;
using Model.Ops.Definition;
using Newtonsoft.Json;
using UnityEngine;

namespace AlinasMapMod.Definitions.Converters
{
  public class LoadConverter : JsonConverter<Load>
  {
    public override void WriteJson(JsonWriter writer, Load value, JsonSerializer serializer)
    {
      writer.WriteValue(value.id);
    }

    public override Load ReadJson(JsonReader reader, Type objectType, Load existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      string s = (string)reader.Value;
      if(existingValue?.id == s) return existingValue;
      return Resources.FindObjectsOfTypeAll<Load>().Single(l => l.id == s);
    }
  }

}