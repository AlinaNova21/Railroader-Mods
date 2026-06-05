using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Serialization;
using Xunit;

namespace AlinasRailTools.Shared.Tests.Serialization;

public class DataSerializerTests
{
  private readonly DataSerializer _serializer = new();

  [Fact]
  public void Serialize_SimpleObject_ProducesValidJson()
  {
    // Arrange
    var node = new SerializedTrackNode
    {
      Position = new SerializedVector3 { X = 1, Y = 2, Z = 3 },
      Rotation = new SerializedVector3 { X = 0, Y = 45, Z = 0 },
      FlipSwitchStand = true
    };

    // Act
    var json = _serializer.Serialize(node);

    // Assert
    Assert.NotNull(json);
    Assert.Contains("\"position\"", json);
    Assert.Contains("\"rotation\"", json);
    Assert.Contains("\"flipSwitchStand\"", json);
  }

  [Fact]
  public void Deserialize_ValidJson_ReturnsObject()
  {
    // Arrange
    var json = @"{
      ""position"": { ""x"": 1, ""y"": 2, ""z"": 3 },
      ""rotation"": { ""x"": 0, ""y"": 45, ""z"": 0 },
      ""flipSwitchStand"": true
    }";

    // Act
    var node = _serializer.Deserialize<SerializedTrackNode>(json);

    // Assert
    Assert.NotNull(node);
    Assert.Equal(1, node.Position.X);
    Assert.Equal(2, node.Position.Y);
    Assert.Equal(3, node.Position.Z);
    Assert.Equal(45, node.Rotation.Y);
    Assert.True(node.FlipSwitchStand);
  }

  [Fact]
  public void RoundTrip_ObjectToJsonToObject_PreservesData()
  {
    // Arrange
    var original = new SerializedTrackNode
    {
      Position = new SerializedVector3 { X = 10, Y = 20, Z = 30 },
      Rotation = new SerializedVector3 { X = 5, Y = 90, Z = 15 },
      FlipSwitchStand = false
    };

    // Act
    var json = _serializer.Serialize(original);
    var deserialized = _serializer.Deserialize<SerializedTrackNode>(json);

    // Assert
    Assert.Equal(original.Position.X, deserialized.Position.X);
    Assert.Equal(original.Position.Y, deserialized.Position.Y);
    Assert.Equal(original.Position.Z, deserialized.Position.Z);
    Assert.Equal(original.Rotation.Y, deserialized.Rotation.Y);
    Assert.Equal(original.FlipSwitchStand, deserialized.FlipSwitchStand);
  }

  [Fact]
  public void Serialize_NullValuesOmitted_ProducesCleanJson()
  {
    // Arrange
    var segment = new SerializedTrackSegment
    {
      StartId = "node-1",
      EndId = "node-2",
      Style = TrackSegmentStyle.Standard,
      GroupId = null // Should be omitted
    };

    // Act
    var json = _serializer.Serialize(segment);

    // Assert
    Assert.DoesNotContain("\"groupId\"", json); // Null should be omitted
  }
}
