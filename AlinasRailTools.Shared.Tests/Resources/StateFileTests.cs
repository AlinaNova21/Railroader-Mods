using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Resources;

public class StateFileTests
{
  [Fact]
  public void FromJson_ValidJson_ParsesCorrectly()
  {
    // Arrange
    var json = @"{
      ""trackNodes"": {
        ""node-1"": { ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 } },
        ""node-2"": { ""position"": { ""x"": 4, ""y"": 5, ""z"": 6 } }
      },
      ""scenery"": {
        ""tree-1"": { ""modelIdentifier"": ""oak"" }
      }
    }";

    // Act
    var state = StateFile.FromJson(json);

    // Assert
    Assert.Equal(2, state.Resources.Count);
    Assert.Equal(2, state.Resources["trackNodes"].Count);
    Assert.Single(state.Resources["scenery"]);
    Assert.NotNull(state.Resources["trackNodes"]["node-1"]);
  }

  [Fact]
  public void FromJson_NullValue_PreservesNull()
  {
    // Arrange
    var json = @"{
      ""trackNodes"": {
        ""node-1"": { ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 } },
        ""node-2"": null
      }
    }";

    // Act
    var state = StateFile.FromJson(json);

    // Assert
    Assert.Equal(2, state.Resources["trackNodes"].Count);
    Assert.NotNull(state.Resources["trackNodes"]["node-1"]);
    Assert.Null(state.Resources["trackNodes"]["node-2"]);
  }

  [Fact]
  public void ToJson_RoundTrip_PreservesData()
  {
    // Arrange
    var state = new StateFile();
    state.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 } }"),
      ["node-2"] = null  // Deletion marker
    };

    // Act
    var json = state.ToJson();
    var restored = StateFile.FromJson(json);

    // Assert
    Assert.Equal(2, restored.Resources["trackNodes"].Count);
    Assert.NotNull(restored.Resources["trackNodes"]["node-1"]);
    Assert.Null(restored.Resources["trackNodes"]["node-2"]);
  }

  [Fact]
  public void Clone_CreatesDeepCopy()
  {
    // Arrange
    var original = new StateFile();
    original.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 } }")
    };

    // Act
    var clone = original.Clone();
    clone.Resources["trackNodes"]["node-1"]["position"]["x"] = 999;

    // Assert
    Assert.Equal(1, original.Resources["trackNodes"]["node-1"]["position"]["x"]);
    Assert.Equal(999, clone.Resources["trackNodes"]["node-1"]["position"]["x"]);
  }

  [Fact]
  public void GetResources_NonExistentType_ReturnsEmpty()
  {
    // Arrange
    var state = new StateFile();

    // Act
    var resources = state.GetResources("nonExistent");

    // Assert
    Assert.Empty(resources);
  }

  [Fact]
  public void SetResources_NullOrEmpty_RemovesType()
  {
    // Arrange
    var state = new StateFile();
    state.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 1 }")
    };

    // Act
    state.SetResources("trackNodes", null);

    // Assert
    Assert.DoesNotContain("trackNodes", state.Resources.Keys);
  }
}
