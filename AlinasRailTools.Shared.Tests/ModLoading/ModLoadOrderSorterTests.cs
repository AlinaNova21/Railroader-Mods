using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.ModLoading;
using Xunit;

namespace AlinasRailTools.Shared.Tests.ModLoading;

public class ModLoadOrderSorterTests
{
  [Fact]
  public void Sort_NoDependencies_ReturnsAllMods()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-a"),
      CreateMod("mod-b"),
      CreateMod("mod-c")
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(3, result.SortedMods.Count);
    Assert.Empty(result.Errors);
    Assert.Empty(result.SkippedMods);
  }

  [Fact]
  public void Sort_SimpleDependency_OrdersCorrectly()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-b", requires: ["mod-a"]),
      CreateMod("mod-a")
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(2, result.SortedMods.Count);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Equal("mod-b", result.SortedMods[1].Definition.Id);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Sort_ChainedDependencies_OrdersCorrectly()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-c", requires: ["mod-b"]),
      CreateMod("mod-a"),
      CreateMod("mod-b", requires: ["mod-a"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(3, result.SortedMods.Count);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Equal("mod-b", result.SortedMods[1].Definition.Id);
    Assert.Equal("mod-c", result.SortedMods[2].Definition.Id);
  }

  [Fact]
  public void Sort_LoadAfter_OrdersCorrectly()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-b", loadAfter: ["mod-a"]),
      CreateMod("mod-a")
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(2, result.SortedMods.Count);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Equal("mod-b", result.SortedMods[1].Definition.Id);
  }

  [Fact]
  public void Sort_LoadBefore_OrdersCorrectly()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-b"),
      CreateMod("mod-a", loadBefore: ["mod-b"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(2, result.SortedMods.Count);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Equal("mod-b", result.SortedMods[1].Definition.Id);
  }

  [Fact]
  public void Sort_MissingDependency_SkipsModAndLogsError()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-a", requires: ["missing-mod"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Empty(result.SortedMods);
    Assert.Single(result.SkippedMods);
    Assert.Equal("mod-a", result.SkippedMods[0].Definition.Id);
    Assert.Single(result.Errors);
    Assert.Equal(ModLoadErrorType.MissingDependency, result.Errors[0].Type);
    Assert.Equal("mod-a", result.Errors[0].ModId);
    Assert.Equal("missing-mod", result.Errors[0].RelatedModId);
  }

  [Fact]
  public void Sort_CircularDependency_DetectsAndSkipsCycle()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-a", requires: ["mod-b"]),
      CreateMod("mod-b", requires: ["mod-a"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Equal(2, result.SkippedMods.Count);
    Assert.Single(result.Errors);
    Assert.Equal(ModLoadErrorType.CircularDependency, result.Errors[0].Type);
    Assert.NotNull(result.Errors[0].Cycle);
    Assert.Contains("mod-a", result.Errors[0].Cycle);
    Assert.Contains("mod-b", result.Errors[0].Cycle);
  }

  [Fact]
  public void Sort_Conflict_SkipsConflictingMod()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-a"),
      CreateMod("mod-b", conflictsWith: ["mod-a"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Single(result.SortedMods);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Single(result.SkippedMods);
    Assert.Equal("mod-b", result.SkippedMods[0].Definition.Id);
    Assert.Single(result.Errors);
    Assert.Equal(ModLoadErrorType.Conflict, result.Errors[0].Type);
  }

  [Fact]
  public void Sort_OptionalLoadAfterMissing_ContinuesWithoutError()
  {
    // Arrange
    var mods = new List<RLModInfo>
    {
      CreateMod("mod-a", loadAfter: ["missing-mod"])
    };
    var sorter = new ModLoadOrderSorter();

    // Act
    var result = sorter.Sort(mods);

    // Assert
    Assert.Single(result.SortedMods);
    Assert.Equal("mod-a", result.SortedMods[0].Definition.Id);
    Assert.Empty(result.Errors);
  }

  // Helper methods
  private static RLModInfo CreateMod(
    string id,
    string[]? requires = null,
    string[]? loadAfter = null,
    string[]? loadBefore = null,
    string[]? conflictsWith = null)
  {
    return new RLModInfo
    {
      Directory = $"/mods/{id}",
      Definition = new RLModDefinition
      {
        Id = id,
        Name = $"Test Mod {id}",
        Version = "1.0.0",
        Requires = requires?.Select(r => new RLModRequirement { Id = r }).ToArray() ?? [],
        LoadAfter = loadAfter?.Select(r => new RLModRequirement { Id = r }).ToArray() ?? [],
        LoadBefore = loadBefore?.Select(r => new RLModRequirement { Id = r }).ToArray() ?? [],
        ConflictsWith = conflictsWith?.Select(r => new RLModRequirement { Id = r }).ToArray() ?? []
      }
    };
  }
}
