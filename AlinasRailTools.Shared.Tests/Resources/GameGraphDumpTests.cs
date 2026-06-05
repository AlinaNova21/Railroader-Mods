using System.IO;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Resources;

public class GameGraphDumpTests
{
  private readonly LegacyConverter _converter = new();

  [Fact]
  public void GameGraphDump_CanBeLoaded()
  {
    // Arrange
    var dumpPath = Path.Combine("TestData", "game-graph-dump.json");
    Assert.True(File.Exists(dumpPath), $"Game graph dump not found at {dumpPath}");

    var json = File.ReadAllText(dumpPath);
    var legacyJson = JObject.Parse(json);

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert
    Assert.NotNull(state);
    Assert.NotEmpty(state.Resources);
  }

  [Fact]
  public void GameGraphDump_ContainsExpectedResourceTypes()
  {
    // Arrange
    var dumpPath = Path.Combine("TestData", "game-graph-dump.json");
    var json = File.ReadAllText(dumpPath);
    var legacyJson = JObject.Parse(json);

    // Act
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert - Verify expected resource types are present
    Assert.Contains("trackNodes", state.Resources.Keys);
    Assert.Contains("trackSegments", state.Resources.Keys);
    Assert.Contains("trackSpans", state.Resources.Keys);
    Assert.Contains("scenery", state.Resources.Keys);
    Assert.Contains("areas", state.Resources.Keys);
    // Note: progressions are in a separate file (progressions-dump.json)
  }

  [Fact]
  public void GameGraphDump_AllIndustryComponentsDeserialize()
  {
    // Arrange
    var dumpPath = Path.Combine("TestData", "game-graph-dump.json");
    var json = File.ReadAllText(dumpPath);
    var legacyJson = JObject.Parse(json);

    // Act - Use our converter system
    var state = _converter.ConvertToStateFile(legacyJson);

    // Assert - Navigate through converted state and deserialize components
    var componentCount = 0;
    var deserializedCount = 0;

    if (state.Resources.ContainsKey("areas"))
    {
      var areas = state.Resources["areas"];

      foreach (var areaEntry in areas)
      {
        var areaData = areaEntry.Value;
        if (areaData?["industries"] is JObject industries)
        {
          foreach (var industryEntry in industries)
          {
            var industryData = industryEntry.Value as JObject;
            if (industryData?["components"] is JObject components)
            {
              foreach (var componentEntry in components)
              {
                var componentData = componentEntry.Value as JObject;
                if (componentData != null)
                {
                  componentCount++;

                  // Test that JsonSubTypes properly deserializes the component
                  var component = componentData.ToObject<SerializedIndustryComponent>();
                  Assert.NotNull(component);
                  Assert.NotNull(component.Type);
                  deserializedCount++;
                }
              }
            }
          }
        }
      }
    }

    // Verify we found and deserialized components
    Assert.True(componentCount > 0, "Expected to find industry components in game graph dump");
    Assert.Equal(componentCount, deserializedCount);
  }

  [Fact]
  public void GameGraphDump_AllComponentTypesAreRecognized()
  {
    // Arrange
    var dumpPath = Path.Combine("TestData", "game-graph-dump.json");
    var json = File.ReadAllText(dumpPath);
    var legacyJson = JObject.Parse(json);

    // Act - Use our converter and collect component types from converted state
    var state = _converter.ConvertToStateFile(legacyJson);
    var componentTypes = new HashSet<string>();

    if (state.Resources.ContainsKey("areas"))
    {
      var areas = state.Resources["areas"];

      foreach (var areaEntry in areas)
      {
        var areaData = areaEntry.Value;
        if (areaData?["industries"] is JObject industries)
        {
          foreach (var industryEntry in industries)
          {
            var industryData = industryEntry.Value as JObject;
            if (industryData?["components"] is JObject components)
            {
              foreach (var componentEntry in components)
              {
                var componentData = componentEntry.Value as JObject;
                if (componentData != null)
                {
                  var typeToken = componentData["type"];
                  if (typeToken?.Type == JTokenType.String)
                  {
                    var typeName = typeToken.Value<string>();
                    if (!string.IsNullOrEmpty(typeName))
                    {
                      componentTypes.Add(typeName);
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    // Assert - All component types should deserialize to concrete types via JsonSubTypes
    Assert.NotEmpty(componentTypes);

    foreach (var typeName in componentTypes)
    {
      var testJson = $@"{{
        ""type"": ""{typeName}"",
        ""subIdentifier"": ""test""
      }}";

      var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(testJson);
      Assert.NotNull(component);

      // Verify it deserialized to a concrete type, not just the base class
      var isConcreteType = component.GetType() != typeof(SerializedIndustryComponent);
      Assert.True(isConcreteType,
        $"Component type '{typeName}' deserializes to base class. May need to add JsonSubTypes mapping.");
    }
  }

  [Fact]
  public void GameGraphDump_RoundTripPreservesData()
  {
    // Arrange
    var dumpPath = Path.Combine("TestData", "game-graph-dump.json");
    var json = File.ReadAllText(dumpPath);
    var legacyJson = JObject.Parse(json);

    // Act - Convert to StateFile and back to JSON
    var state = _converter.ConvertToStateFile(legacyJson);
    var stateJson = state.ToJson();
    var reloadedState = StateFile.FromJson(stateJson);

    // Assert - Verify resource counts match
    Assert.Equal(state.Resources.Count, reloadedState.Resources.Count);

    foreach (var typeEntry in state.Resources)
    {
      var typeName = typeEntry.Key;
      var resources = typeEntry.Value;

      Assert.Contains(typeName, reloadedState.Resources.Keys);
      Assert.Equal(resources.Count, reloadedState.Resources[typeName].Count);
    }
  }
}
