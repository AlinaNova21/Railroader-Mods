using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasMapMod.Definitions.Converters;

internal sealed class Vector3Converter : JsonConverter<Vector3>
{
  public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Null) return Vector3.zero;
    var obj = JObject.FromObject(reader.Value!);
    if (obj.ContainsKey("x")) existingValue.x = (float)obj["x"];
    if (obj.ContainsKey("y")) existingValue.y = (float)obj["y"];
    if (obj.ContainsKey("z")) existingValue.z = (float)obj["z"];
    return existingValue;
  }

  public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
  {
    var obj = new JObject
    {
      ["x"] = value.x,
      ["y"] = value.y,
      ["z"] = value.z
    };
    var formatting1 = writer.Formatting;
    var formatting2 = serializer.Formatting;
    writer.Formatting = Formatting.None;
    serializer.Formatting = Formatting.None;
    serializer.Serialize(writer, obj);
    serializer.Formatting = formatting2;
    writer.Formatting = formatting1;
  }
}
