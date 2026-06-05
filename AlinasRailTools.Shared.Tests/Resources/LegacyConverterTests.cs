using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Resources;

public class LegacyConverterTests
{
  private readonly LegacyConverter _converter = new();

  [Fact]
  public void ConvertToStateFile_TracksNodes_ConvertsToCamelCase()
  {
    // Arrange
    var legacyJson = JObject.Parse(@"{
      ""tracks"": {
        ""nodes"": {
          ""node-1"": { ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 } },
          ""node-2"": { ""position"": { ""x"": 4, ""y"": 5, ""z"": 6 } }
        }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Contains("trackNodes", state.Resources.Keys);
    Assert.Equal(2, state.Resources["trackNodes"].Count);
    Assert.NotNull(state.Resources["trackNodes"]["node-1"]);
  }

  [Fact]
  public void ConvertToStateFile_TracksSegments_ConvertsToCamelCase()
  {
    // Arrange
    var legacyJson = JObject.Parse(@"{
      ""tracks"": {
        ""segments"": {
          ""seg-1"": { ""startId"": ""node-1"", ""endId"": ""node-2"" }
        }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Contains("trackSegments", state.Resources.Keys);
    Assert.Single(state.Resources["trackSegments"]);
  }

  [Fact]
  public void ConvertToStateFile_NullValue_PreservesAsNull()
  {
    // Arrange
    var legacyJson = JObject.Parse(@"{
      ""tracks"": {
        ""nodes"": {
          ""node-1"": { ""position"": { ""x"": 1 } },
          ""node-2"": null
        }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Equal(2, state.Resources["trackNodes"].Count);
    Assert.NotNull(state.Resources["trackNodes"]["node-1"]);
    Assert.Null(state.Resources["trackNodes"]["node-2"]);
  }

  [Fact]
  public void ConvertToStateFile_Scenery_KeepsName()
  {
    // Arrange
    var legacyJson = JObject.Parse(@"{
      ""scenery"": {
        ""tree-1"": { ""modelIdentifier"": ""oak"" }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Contains("scenery", state.Resources.Keys);
    Assert.Single(state.Resources["scenery"]);
  }

  [Fact]
  public void ConvertToStateFile_MultipleTypes_ConvertsAll()
  {
    // Arrange
    var legacyJson = JObject.Parse(@"{
      ""tracks"": {
        ""nodes"": {
          ""node-1"": { ""x"": 1 }
        },
        ""segments"": {
          ""seg-1"": { ""x"": 2 }
        }
      },
      ""scenery"": {
        ""tree-1"": { ""x"": 3 }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Equal(3, state.Resources.Count);
    Assert.Contains("trackNodes", state.Resources.Keys);
    Assert.Contains("trackSegments", state.Resources.Keys);
    Assert.Contains("scenery", state.Resources.Keys);
  }

  [Fact]
  public void ConvertToStateFile_PassengerStop_RemapsFromOldNamespace()
  {
    // Arrange - Old legacy format with AlinasMapMod.Ops.PassengerStop
    var legacyJson = JObject.Parse(@"{
      ""areas"": {
        ""whittier-station"": {
          ""identifier"": ""whittier-station"",
          ""displayName"": ""Whittier Station"",
          ""components"": [
            {
              ""type"": ""AlinasMapMod.Ops.PassengerStop"",
              ""subIdentifier"": ""stop-1"",
              ""identifier"": ""whittier-station"",
              ""displayName"": ""Whittier Station"",
              ""position"": { ""x"": 100, ""y"": 0, ""z"": 200 }
            }
          ]
        }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Contains("areas", state.Resources.Keys);
    var station = state.Resources["areas"]["whittier-station"];
    Assert.NotNull(station);

    var components = station["components"] as JArray;
    Assert.NotNull(components);
    Assert.Single(components);

    var component = components[0] as JObject;
    Assert.NotNull(component);

    // Verify the type was remapped to base game namespace
    Assert.Equal("Model.Ops.PassengerStop", component["type"]?.Value<string>());
  }

  [Fact]
  public void ConvertToStateFile_PassengerStop_KeepsNewNamespace()
  {
    // Arrange - New format already using Model.Ops.PassengerStop
    var legacyJson = JObject.Parse(@"{
      ""areas"": {
        ""sylva-station"": {
          ""identifier"": ""sylva-station"",
          ""displayName"": ""Sylva Station"",
          ""components"": [
            {
              ""type"": ""Model.Ops.PassengerStop"",
              ""subIdentifier"": ""stop-1"",
              ""identifier"": ""sylva-station"",
              ""displayName"": ""Sylva Station"",
              ""position"": { ""x"": 200, ""y"": 0, ""z"": 300 }
            }
          ]
        }
      }
    }");

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.Contains("areas", state.Resources.Keys);
    var station = state.Resources["areas"]["sylva-station"];
    Assert.NotNull(station);

    var components = station["components"] as JArray;
    Assert.NotNull(components);
    Assert.Single(components);

    var component = components[0] as JObject;
    Assert.NotNull(component);

    // Verify the type remains unchanged
    Assert.Equal("Model.Ops.PassengerStop", component["type"]?.Value<string>());
  }
}
