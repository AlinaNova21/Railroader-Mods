using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Resources;

public class StateMergerTests
{
  private readonly StateMerger _merger = new();

  [Fact]
  public void Overlay_AddNewResource_AddsToResult()
  {
    // Arrange
    var baseState = new StateFile();
    baseState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 1 }")
    };

    var newState = new StateFile();
    newState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-2"] = JObject.Parse(@"{ ""x"": 2 }")
    };

    // Act
    var result = _merger.Overlay(baseState, newState);

    // Assert
    Assert.Equal(2, result.Resources["trackNodes"].Count);
    Assert.NotNull(result.Resources["trackNodes"]["node-1"]);
    Assert.NotNull(result.Resources["trackNodes"]["node-2"]);
  }

  [Fact]
  public void Overlay_UpdateExistingResource_Merges()
  {
    // Arrange
    var baseState = new StateFile();
    baseState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 1, ""y"": 2 }")
    };

    var newState = new StateFile();
    newState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""y"": 999 }")  // Only update y
    };

    // Act
    var result = _merger.Overlay(baseState, newState);

    // Assert
    Assert.Single(result.Resources["trackNodes"]);
    Assert.Equal(1, (int)result.Resources["trackNodes"]["node-1"]["x"]);
    Assert.Equal(999, (int)result.Resources["trackNodes"]["node-1"]["y"]);
  }

  [Fact]
  public void Overlay_NullValue_RemovesResource()
  {
    // Arrange
    var baseState = new StateFile();
    baseState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 1 }"),
      ["node-2"] = JObject.Parse(@"{ ""x"": 2 }")
    };

    var newState = new StateFile();
    newState.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = null  // Delete node-1
    };

    // Act
    var result = _merger.Overlay(baseState, newState);

    // Assert
    Assert.Single(result.Resources["trackNodes"]);
    Assert.DoesNotContain("node-1", result.Resources["trackNodes"].Keys);
    Assert.Contains("node-2", result.Resources["trackNodes"].Keys);
  }

  [Fact]
  public void Overlay_DeepMerge_PreservesNestedStructure()
  {
    // Arrange
    var baseState = new StateFile();
    baseState.Resources["areas"] = new Dictionary<string, JObject>
    {
      ["area-1"] = JObject.Parse(@"{
        ""identifier"": ""whittier"",
        ""industries"": {
          ""sawmill"": { ""name"": ""Sawmill"", ""capacity"": 100 }
        }
      }")
    };

    var newState = new StateFile();
    newState.Resources["areas"] = new Dictionary<string, JObject>
    {
      ["area-1"] = JObject.Parse(@"{
        ""industries"": {
          ""sawmill"": { ""capacity"": 200 },
          ""warehouse"": { ""name"": ""Warehouse"" }
        }
      }")
    };

    // Act
    var result = _merger.Overlay(baseState, newState);

    // Assert
    var area = result.Resources["areas"]["area-1"];
    Assert.Equal("whittier", (string)area["identifier"]);
    Assert.Equal(200, (int)area["industries"]["sawmill"]["capacity"]);
    Assert.Equal("Sawmill", (string)area["industries"]["sawmill"]["name"]);
    Assert.Equal("Warehouse", (string)area["industries"]["warehouse"]["name"]);
  }

  [Fact]
  public void Overlay_ArrayReplacement_ReplacesNotMerges()
  {
    // Arrange
    var baseState = new StateFile();
    baseState.Resources["test"] = new Dictionary<string, JObject>
    {
      ["item-1"] = JObject.Parse(@"{ ""tags"": [""a"", ""b""] }")
    };

    var newState = new StateFile();
    newState.Resources["test"] = new Dictionary<string, JObject>
    {
      ["item-1"] = JObject.Parse(@"{ ""tags"": [""c""] }")
    };

    // Act
    var result = _merger.Overlay(baseState, newState);

    // Assert
    var tags = result.Resources["test"]["item-1"]["tags"] as JArray;
    Assert.Single(tags);
    Assert.Equal("c", (string)tags[0]);
  }

  [Fact]
  public void Overlay_MultipleFiles_AppliesSequentially()
  {
    // Arrange
    var base1 = new StateFile();
    base1.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 1 }")
    };

    var mod1 = new StateFile();
    mod1.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-2"] = JObject.Parse(@"{ ""x"": 2 }")
    };

    var mod2 = new StateFile();
    mod2.Resources["trackNodes"] = new Dictionary<string, JObject>
    {
      ["node-1"] = JObject.Parse(@"{ ""x"": 999 }")  // Override
    };

    // Act
    var result = _merger.Overlay(base1, mod1);
    result = _merger.Overlay(result, mod2);

    // Assert
    Assert.Equal(2, result.Resources["trackNodes"].Count);
    Assert.Equal(999, (int)result.Resources["trackNodes"]["node-1"]["x"]);
    Assert.Equal(2, (int)result.Resources["trackNodes"]["node-2"]["x"]);
  }
}
