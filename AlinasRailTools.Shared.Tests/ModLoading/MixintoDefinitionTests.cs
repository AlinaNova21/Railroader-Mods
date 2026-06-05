using AlinasRailTools.Shared.ModLoading;
using Newtonsoft.Json;
using Xunit;

namespace AlinasRailTools.Shared.Tests.ModLoading;

public class MixintoDefinitionTests
{
  [Fact]
  public void Deserialize_SingleString_CreatesListWithOneItem()
  {
    // Arrange
    var json = @"{
      ""game-graph"": ""file(game-graph.json)""
    }";

    // Act
    var mixintos = JsonConvert.DeserializeObject<MixintoDefinitions>(json);

    // Assert
    Assert.NotNull(mixintos);
    Assert.Single(mixintos);
    Assert.Single(mixintos["game-graph"]);
    Assert.Equal("game-graph.json", mixintos["game-graph"][0].File);
  }

  [Fact]
  public void Deserialize_ArrayOfStrings_CreatesListWithMultipleItems()
  {
    // Arrange
    var json = @"{
      ""game-graph"": [
        ""file(graph1.json)"",
        ""file(graph2.json)"",
        ""file(graph3.json)""
      ]
    }";

    // Act
    var mixintos = JsonConvert.DeserializeObject<MixintoDefinitions>(json);

    // Assert
    Assert.NotNull(mixintos);
    Assert.Equal(3, mixintos["game-graph"].Count);
    Assert.Equal("graph1.json", mixintos["game-graph"][0].File);
    Assert.Equal("graph2.json", mixintos["game-graph"][1].File);
    Assert.Equal("graph3.json", mixintos["game-graph"][2].File);
  }

  [Fact]
  public void Deserialize_MultipleTypes_HandlesEachCorrectly()
  {
    // Arrange
    var json = @"{
      ""game-graph"": ""file(graph.json)"",
      ""progressions"": [""file(prog1.json)"", ""file(prog2.json)""],
      ""container:custom"": ""file(container.json)""
    }";

    // Act
    var mixintos = JsonConvert.DeserializeObject<MixintoDefinitions>(json);

    // Assert
    Assert.NotNull(mixintos);
    Assert.Equal(3, mixintos.Count);
    Assert.Single(mixintos["game-graph"]);
    Assert.Equal(2, mixintos["progressions"].Count);
    Assert.Single(mixintos["container:custom"]);
  }

  [Fact]
  public void GetGameGraphs_ReturnsAllGameGraphMixintos()
  {
    // Arrange
    var mixintos = new MixintoDefinitions
    {
      ["game-graph"] = [new MixintoReference { File = "graph1.json" }],
      ["game-graph-2"] = [new MixintoReference { File = "graph2.json" }],
      ["progressions"] = [new MixintoReference { File = "prog.json" }]
    };

    // Act
    var gameGraphs = mixintos.GetGameGraphs().ToList();

    // Assert
    Assert.Equal(2, gameGraphs.Count);
    Assert.Contains(gameGraphs, g => g.File == "graph1.json");
    Assert.Contains(gameGraphs, g => g.File == "graph2.json");
  }

  [Fact]
  public void GetProgressions_ReturnsAllProgressionMixintos()
  {
    // Arrange
    var mixintos = new MixintoDefinitions
    {
      ["progressions"] = [new MixintoReference { File = "prog1.json" }],
      ["progressions-extra"] = [new MixintoReference { File = "prog2.json" }],
      ["game-graph"] = [new MixintoReference { File = "graph.json" }]
    };

    // Act
    var progressions = mixintos.GetProgressions().ToList();

    // Assert
    Assert.Equal(2, progressions.Count);
  }

  [Fact]
  public void GetContainers_ReturnsContainerNamesAndReferences()
  {
    // Arrange
    var mixintos = new MixintoDefinitions
    {
      ["container:custom-tracks"] = [new MixintoReference { File = "custom.json" }],
      ["container:special"] = [new MixintoReference { File = "special.json" }],
      ["game-graph"] = [new MixintoReference { File = "graph.json" }]
    };

    // Act
    var containers = mixintos.GetContainers().ToList();

    // Assert
    Assert.Equal(2, containers.Count);
    Assert.Contains(containers, c => c.ContainerName == "custom-tracks");
    Assert.Contains(containers, c => c.ContainerName == "special");
  }

  [Fact]
  public void Serialize_WriteAsArrays_ProducesConsistentFormat()
  {
    // Arrange
    var mixintos = new MixintoDefinitions
    {
      ["game-graph"] = [new MixintoReference { File = "graph.json" }],
      ["progressions"] = [
        new MixintoReference { File = "prog1.json" },
        new MixintoReference { File = "prog2.json" }
      ]
    };

    // Act
    var json = JsonConvert.SerializeObject(mixintos, Formatting.Indented);

    // Assert
    Assert.Contains("\"game-graph\": [", json);
    Assert.Contains("\"progressions\": [", json);
    Assert.Contains("file(graph.json)", json);
    Assert.Contains("file(prog1.json)", json);
  }

  [Fact]
  public void MixintoReference_ToString_ReturnsFileFormat()
  {
    // Arrange
    var reference = new MixintoReference { File = "test.json" };

    // Act
    var result = reference.ToString();

    // Assert
    Assert.Equal("file(test.json)", result);
  }

  [Fact]
  public void MixintoReference_FromString_ParsesFileFormat()
  {
    // Arrange
    var input = "file(my-file.json)";

    // Act
    var reference = MixintoReference.FromString(input);

    // Assert
    Assert.Equal("my-file.json", reference.File);
  }

  [Fact]
  public void MixintoReference_FromString_HandlesPlainPath()
  {
    // Arrange
    var input = "plain-path.json";

    // Act
    var reference = MixintoReference.FromString(input);

    // Assert
    Assert.Equal("plain-path.json", reference.File);
  }

  [Fact]
  public void RoundTrip_ComplexMixintos_PreservesStructure()
  {
    // Arrange
    var original = new MixintoDefinitions
    {
      ["game-graph"] = [new MixintoReference { File = "g1.json" }],
      ["game-graph-2"] = [new MixintoReference { File = "g2.json" }],
      ["progressions"] = [
        new MixintoReference { File = "p1.json" },
        new MixintoReference { File = "p2.json" }
      ],
      ["container:tracks"] = [new MixintoReference { File = "tracks.json" }]
    };

    // Act
    var json = JsonConvert.SerializeObject(original);
    var deserialized = JsonConvert.DeserializeObject<MixintoDefinitions>(json);

    // Assert
    Assert.Equal(original.Count, deserialized!.Count);
    Assert.Equal(original["game-graph"].Count, deserialized["game-graph"].Count);
    Assert.Equal(original["progressions"].Count, deserialized["progressions"].Count);
  }
}
