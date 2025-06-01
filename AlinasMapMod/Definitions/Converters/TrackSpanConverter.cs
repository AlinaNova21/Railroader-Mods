using System;
using System.Linq;
using Newtonsoft.Json;
using Track;

namespace AlinasMapMod.Definitions.Converters;

public class TrackSpanConverter : JsonConverter<TrackSpan>
{
  public override void WriteJson(JsonWriter writer, TrackSpan value, JsonSerializer serializer)
  {
    writer.WriteValue(value.id);
  }

  public override TrackSpan ReadJson(JsonReader reader, Type objectType, TrackSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    string s = (string)reader.Value;
    if (existingValue?.id == s) return existingValue;

    var ret = UnityEngine.Object.FindObjectsOfType<TrackSpan>(true).Single(ts => ts.id == s);
    return ret;
  }
}
