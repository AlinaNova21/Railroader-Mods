using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasMapMod.Definitions.Converters;

internal sealed class Vector2Converter : JsonConverter<Vector2>
{
  public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Null) return Vector2.zero;
    var obj = JObject.FromObject(reader.Value!);
    if (obj.ContainsKey("x")) existingValue.x = (float)obj["x"];
    if (obj.ContainsKey("y")) existingValue.y = (float)obj["y"];

    return existingValue;
  }

  public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
  {
    var obj = new JObject
    {
      ["x"] = value.x,
      ["y"] = value.y,
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
