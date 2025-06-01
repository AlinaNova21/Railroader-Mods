using System.Collections.Generic;
using System.IO;
using AlinasMapMod.Definitions.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AlinasMapMod.Definitions;

public class MapModDef
{
  public Dictionary<string, MapModItem> Items { get; set; } = new Dictionary<string, MapModItem>();
  public Dictionary<string, SerializedTurntable> Turntables { get; set; } = new Dictionary<string, SerializedTurntable>();

  public static MapModDef Parse(string file)
  {
    var jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
    {
      ContractResolver = new DefaultContractResolver
      {
        NamingStrategy = new CamelCaseNamingStrategy
        {
          ProcessDictionaryKeys = false
        }
      },
      Converters = [
            new LoadConverter(),
                  new TrackSpanConverter(),
                  new CarTypeFilterConverter(),
                  new ProgressionIndustryComponentConverter(),
              ]
    });

    var obj = JObject.Parse(File.ReadAllText(file)).ToObject<MapModDef>(jsonSerializer);
    if (obj == null) {
      throw new JsonException("Failed to parse map mod definition");
    }
    return obj;
  }
}
