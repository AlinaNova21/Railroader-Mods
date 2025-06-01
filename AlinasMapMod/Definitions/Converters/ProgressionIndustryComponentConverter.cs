using System;
using System.Linq;
using Model.Ops;
using Newtonsoft.Json;

namespace AlinasMapMod.Definitions.Converters;

public class ProgressionIndustryComponentConverter : JsonConverter<ProgressionIndustryComponent>
{
  public override void WriteJson(JsonWriter writer, ProgressionIndustryComponent value, JsonSerializer serializer)
  {
    writer.WriteValue(value.Identifier);
  }

  public override ProgressionIndustryComponent ReadJson(JsonReader reader, Type objectType, ProgressionIndustryComponent existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    string s = (string)reader.Value;
    if (existingValue?.Identifier == s) return existingValue;
    var industryComponent = UnityEngine.Object.FindObjectsOfType<ProgressionIndustryComponent>(true).Single(ts => ts.Identifier == s);
    if (industryComponent == null || industryComponent == default) {
      throw new Exception($"Industry component with id {s} not found");
    }
    return industryComponent;
  }
}
