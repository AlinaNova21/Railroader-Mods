using System;
using Newtonsoft.Json;

namespace AlinasMapMod.Definitions.Converters;

internal class InlineJsonConverter : JsonConverter
{
  private bool disabled = false;
  public override bool CanRead => false;
  public override bool CanWrite => !disabled;

  public override bool CanConvert(Type objectType)
  {
    return true;
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    lock (this) {
      disabled = true;
      var formatting1 = writer.Formatting;
      var formatting2 = serializer.Formatting;
      writer.Formatting = Formatting.None;
      serializer.Formatting = Formatting.None;
      serializer.Serialize(writer, value);
      serializer.Formatting = formatting2;
      writer.Formatting = formatting1;
      disabled = false;
    }
  }
}
