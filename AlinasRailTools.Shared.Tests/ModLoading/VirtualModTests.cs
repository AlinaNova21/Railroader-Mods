using System.Linq;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.ModLoading;
using Xunit;

namespace AlinasRailTools.Shared.Tests.ModLoading;

public class VirtualModTests
{
  [Fact]
  public void VirtualModInfo_IsVirtual_ReturnsTrue()
  {
    // Arrange & Act
    var virtualMod = VirtualModInfo.Create(new RLModDefinition
    {
      Id = "test.virtual",
      Name = "Test Virtual Mod"
    });

    // Assert
    Assert.True(virtualMod.IsVirtual);
    Assert.Null(virtualMod.Directory);
  }

  [Fact]
  public void CreateStrangeCustomsShim_HasCorrectProperties()
  {
    // Act
    var shim = VirtualModInfo.CreateStrangeCustomsShim();

    // Assert
    Assert.Equal("pasenger.strangecustoms", shim.Definition.Id);
    Assert.Contains("Strange Customs", shim.Definition.Name);
    Assert.Contains("Virtual", shim.Definition.Name);

    // Should require ART
    Assert.Contains(shim.Definition.Requires, r => r.Id == "alinas.railtools");

    // Should conflict with real StrangeCustoms
    Assert.Contains(shim.Definition.ConflictsWith, c => c.Id == "pasenger.strangecustoms");
  }

  [Fact]
  public void CreateAlinasMapModShim_HasCorrectProperties()
  {
    // Act
    var shim = VirtualModInfo.CreateAlinasMapModShim();

    // Assert
    Assert.Equal("cc.alinas.map", shim.Definition.Id);
    Assert.Contains("Alinas Map Mod", shim.Definition.Name);
    Assert.Contains("Virtual", shim.Definition.Name);

    // Should require ART
    Assert.Contains(shim.Definition.Requires, r => r.Id == "alinas.railtools");

    // Should conflict with real AlinasMapMod
    Assert.Contains(shim.Definition.ConflictsWith, c => c.Id == "cc.alinas.map");
  }

  [Fact]
  public void ModLoader_RegisterVirtualMod_IncludesInDiscovery()
  {
    // Arrange
    var loader = new ModLoader("/nonexistent");
    var virtualMod = VirtualModInfo.Create(new RLModDefinition
    {
      Id = "test.virtual",
      Name = "Test Mod"
    });

    // Act
    loader.RegisterVirtualMod(virtualMod);
    var mods = loader.DiscoverMods();

    // Assert
    Assert.Contains(mods, m => m.Definition.Id == "test.virtual");
  }

  [Fact]
  public void ModLoader_RegisterStandardVirtualMods_RegistersBothShims()
  {
    // Arrange
    var loader = new ModLoader("/nonexistent");

    // Act
    loader.RegisterStandardVirtualMods();
    var mods = loader.DiscoverMods();

    // Assert
    Assert.Contains(mods, m => m.Definition.Id == "pasenger.strangecustoms");
    Assert.Contains(mods, m => m.Definition.Id == "cc.alinas.map");
  }

  [Fact]
  public void ModLoader_VirtualModsYieldedFirst()
  {
    // Arrange
    var loader = new ModLoader("/nonexistent");
    var virtualMod = VirtualModInfo.Create(new RLModDefinition
    {
      Id = "test.virtual",
      Name = "Test Mod"
    });

    // Act
    loader.RegisterVirtualMod(virtualMod);
    var mods = loader.DiscoverMods().ToList();

    // Assert
    Assert.NotEmpty(mods);
    Assert.Equal("test.virtual", mods[0].Definition.Id);
  }

  [Fact]
  public void ModLoadOrderSorter_HandlesVirtualModDependencies()
  {
    // Arrange
    var sorter = new ModLoadOrderSorter();

    var artMod = new RLModInfo
    {
      Directory = "/fake/art",
      Definition = new RLModDefinition
      {
        Id = "alinas.railtools",
        Name = "AlinasRailTools",
        Requires = new RLModRequirement[0],
        LoadAfter = new RLModRequirement[0],
        LoadBefore = new RLModRequirement[0],
        ConflictsWith = new RLModRequirement[0]
      }
    };

    var virtualMod = VirtualModInfo.CreateStrangeCustomsShim();

    var dependentMod = new RLModInfo
    {
      Directory = "/fake/dependent",
      Definition = new RLModDefinition
      {
        Id = "test.dependent",
        Name = "Dependent Mod",
        Requires = new[]
        {
          new RLModRequirement { Id = "pasenger.strangecustoms" }
        },
        LoadAfter = new RLModRequirement[0],
        LoadBefore = new RLModRequirement[0],
        ConflictsWith = new RLModRequirement[0]
      }
    };

    // Act
    var result = sorter.Sort(new[] { dependentMod, virtualMod, artMod });

    // Assert
    Assert.False(result.HasErrors);
    Assert.Equal(3, result.SortedMods.Count);

    // ART should load first, then virtual mod, then dependent
    var artIndex = result.SortedMods.FindIndex(m => m.Definition.Id == "alinas.railtools");
    var virtualIndex = result.SortedMods.FindIndex(m => m.Definition.Id == "pasenger.strangecustoms");
    var dependentIndex = result.SortedMods.FindIndex(m => m.Definition.Id == "test.dependent");

    Assert.True(artIndex < virtualIndex, "ART should load before virtual mod");
    Assert.True(virtualIndex < dependentIndex, "Virtual mod should load before dependent");
  }
}
