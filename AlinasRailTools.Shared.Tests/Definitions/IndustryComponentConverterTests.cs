using AlinasRailTools.Shared.Definitions;
using Newtonsoft.Json;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Definitions;

public class IndustryComponentConverterTests
{
  [Fact]
  public void Deserialize_LoaderType_ReturnsSerializedIndustryLoader()
  {
    // Arrange
    var json = @"{
      ""type"": ""Model.Ops.IndustryLoader"",
      ""subIdentifier"": ""loader-1"",
      ""load"": ""lumber"",
      ""productionRate"": 10.5,
      ""maxStorage"": 100,
      ""orderEmpties"": true
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.IsType<SerializedIndustryLoader>(component);
    var loader = (SerializedIndustryLoader)component;
    Assert.Equal("Model.Ops.IndustryLoader", loader.Type);
    Assert.Equal("loader-1", loader.SubIdentifier);
    Assert.Equal("lumber", loader.Load);
    Assert.Equal(10.5f, loader.ProductionRate);
    Assert.Equal(100f, loader.MaxStorage);
    Assert.True(loader.OrderEmpties);
  }

  [Fact]
  public void Deserialize_InterchangedLoaderType_ReturnsSerializedInterchangedIndustryLoader()
  {
    // Arrange
    var json = @"{
      ""type"": ""Model.Ops.InterchangedIndustryLoader"",
      ""subIdentifier"": ""interchange-1"",
      ""load"": ""coal"",
      ""productionRate"": 5.0,
      ""maxStorage"": 50,
      ""orderEmpties"": false,
      ""targetIndustries"": [""industry-1"", ""industry-2""]
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.IsType<SerializedInterchangedIndustryLoader>(component);
    var loader = (SerializedInterchangedIndustryLoader)component;
    Assert.Equal("Model.Ops.InterchangedIndustryLoader", loader.Type);
    Assert.Equal(2, loader.TargetIndustries.Length);
    Assert.Contains("industry-1", loader.TargetIndustries);
  }

  [Fact]
  public void Deserialize_UnloaderType_ReturnsSerializedIndustryUnloader()
  {
    // Arrange
    var json = @"{
      ""type"": ""Model.Ops.IndustryUnloader"",
      ""subIdentifier"": ""unloader-1"",
      ""load"": ""fuel-oil"",
      ""carUnloadRate"": 15.0,
      ""storageConsumptionRate"": 2.0,
      ""maxStorage"": 200,
      ""orderAwayEmpties"": true,
      ""orderLoads"": false
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.IsType<SerializedIndustryUnloader>(component);
    var unloader = (SerializedIndustryUnloader)component;
    Assert.Equal("Model.Ops.IndustryUnloader", unloader.Type);
    Assert.Equal("fuel-oil", unloader.Load);
    Assert.Equal(15f, unloader.CarUnloadRate);
    Assert.True(unloader.OrderAwayEmpties);
    Assert.False(unloader.OrderLoads);
  }

  [Fact]
  public void Deserialize_TeamTrackType_ReturnsSerializedTeamTrack()
  {
    // Arrange
    var json = @"{
      ""type"": ""Model.Ops.TeamTrack"",
      ""subIdentifier"": ""team-1"",
      ""profileName"": ""standard-team-track""
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.IsType<SerializedTeamTrack>(component);
    var teamTrack = (SerializedTeamTrack)component;
    Assert.Equal("Model.Ops.TeamTrack", teamTrack.Type);
    Assert.Equal("standard-team-track", teamTrack.ProfileName);
  }

  [Fact]
  public void Deserialize_PassengerStopType_ReturnsSerializedPassengerStop()
  {
    // Arrange
    var json = @"{
      ""type"": ""Model.Ops.PassengerStop"",
      ""subIdentifier"": ""stop-1"",
      ""identifier"": ""whittier-station"",
      ""displayName"": ""Whittier Station"",
      ""position"": { ""x"": 100, ""y"": 0, ""z"": 200 }
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.IsType<SerializedPassengerStop>(component);
    var stop = (SerializedPassengerStop)component;
    Assert.Equal("Model.Ops.PassengerStop", stop.Type);
    Assert.Equal("Whittier Station", stop.DisplayName);
    Assert.Equal(100, stop.Position.X);
  }

  [Fact]
  public void Serialize_IndustryLoader_ProducesCorrectJson()
  {
    // Arrange
    var loader = new SerializedIndustryLoader
    {
      Type = "Model.Ops.IndustryLoader",
      SubIdentifier = "loader-1",
      Load = "lumber",
      ProductionRate = 10.5f,
      MaxStorage = 100f,
      OrderEmpties = true
    };

    // Act
    var json = JsonConvert.SerializeObject(loader, Formatting.Indented);

    // Assert
    Assert.Contains("\"type\": \"Model.Ops.IndustryLoader\"", json);
    Assert.Contains("\"load\": \"lumber\"", json);
    Assert.Contains("\"productionRate\": 10.5", json);
  }

  [Fact]
  public void RoundTrip_PolymorphicComponents_PreservesTypes()
  {
    // Arrange
    var components = new SerializedIndustryComponent[]
    {
      new SerializedIndustryLoader { Type = "Model.Ops.IndustryLoader", SubIdentifier = "l1", Load = "coal" },
      new SerializedIndustryUnloader { Type = "Model.Ops.IndustryUnloader", SubIdentifier = "u1", Load = "coal" },
      new SerializedTeamTrack { Type = "Model.Ops.TeamTrack", SubIdentifier = "t1", ProfileName = "standard" }
    };

    // Act
    var json = JsonConvert.SerializeObject(components);
    var deserialized = JsonConvert.DeserializeObject<SerializedIndustryComponent[]>(json);

    // Assert
    Assert.Equal(3, deserialized!.Length);
    Assert.IsType<SerializedIndustryLoader>(deserialized[0]);
    Assert.IsType<SerializedIndustryUnloader>(deserialized[1]);
    Assert.IsType<SerializedTeamTrack>(deserialized[2]);
  }

  [Fact]
  public void Deserialize_UnknownType_ReturnsBaseType()
  {
    // Arrange
    var json = @"{
      ""type"": ""unknown-component-type"",
      ""subIdentifier"": ""unknown-1""
    }";

    // Act
    var component = JsonConvert.DeserializeObject<SerializedIndustryComponent>(json);

    // Assert
    Assert.NotNull(component);
    Assert.Equal(typeof(SerializedIndustryComponent), component.GetType());
    Assert.Equal("unknown-component-type", component.Type);
  }
}
