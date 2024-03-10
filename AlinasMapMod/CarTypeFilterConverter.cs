using System;
using System.Linq;
using Model.OpsNew;
using Newtonsoft.Json;
using UnityEngine;

namespace AlinasMapMod
{
  public class CarTypeFilterConverter : JsonConverter<CarTypeFilter>
  {
    public override void WriteJson(JsonWriter writer, CarTypeFilter value, JsonSerializer serializer)
    {
      writer.WriteValue(value.queryString);
    }

    public override CarTypeFilter ReadJson(JsonReader reader, Type objectType, CarTypeFilter existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      string s = (string)reader.Value;
      return new CarTypeFilter(s);
    }
  }

}