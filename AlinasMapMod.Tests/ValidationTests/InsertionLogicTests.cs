
namespace AlinasMapMod.Tests.ValidationTests;

[TestClass]
public class InsertionLogicTests
{
  private class MockStation
  {
    public int SiblingIndex { get; set; }
    public string Name { get; set; }
  }

  [TestMethod]
  public void TestInsertionOrder()
  {
    // Initial sibling indices: 1000, 1200, 1300, 1400, 2300
    var stations = new List<MockStation>
    {
      new MockStation { SiblingIndex = 1000, Name = "A" },
      new MockStation { SiblingIndex = 1200, Name = "B" },
      new MockStation { SiblingIndex = 1300, Name = "C" },
      new MockStation { SiblingIndex = 1400, Name = "D" },
      new MockStation { SiblingIndex = 2300, Name = "E" }
    };

    int newSiblingIndex = 1250;
    var newStation = new MockStation { SiblingIndex = newSiblingIndex, Name = "NEW" };

    // Logic from PaxStationComponent.cs:
    // var newIndex = branch.stations.FindIndex(s =>
    //   (s.passengerStop?.transform.GetComponentInParent<Area>()?.transform.GetSiblingIndex() ?? int.MaxValue) >
    //   (paxStop.transform.GetComponentInParent<Area>()?.transform.GetSiblingIndex() ?? int.MaxValue));
            
    var newIndex = stations.FindIndex(s => s.SiblingIndex > newSiblingIndex);

    if (newIndex == -1)
      stations.Add(newStation);
    else
      stations.Insert(newIndex, newStation);

    // Expected order: 1000, 1200, 1250, 1300, 1400, 2300
    Assert.AreEqual(6, stations.Count);
    Assert.AreEqual(1000, stations[0].SiblingIndex);
    Assert.AreEqual(1200, stations[1].SiblingIndex);
    Assert.AreEqual(1250, stations[2].SiblingIndex);
    Assert.AreEqual("NEW", stations[2].Name);
    Assert.AreEqual(1300, stations[3].SiblingIndex);
    Assert.AreEqual(1400, stations[4].SiblingIndex);
    Assert.AreEqual(2300, stations[5].SiblingIndex);
  }

  [TestMethod]
  public void TestInsertionAtEnd()
  {
    var stations = new List<MockStation>
    {
      new MockStation { SiblingIndex = 1000, Name = "A" }
    };

    int newSiblingIndex = 2000;
    var newStation = new MockStation { SiblingIndex = newSiblingIndex, Name = "NEW" };

    var newIndex = stations.FindIndex(s => s.SiblingIndex > newSiblingIndex);

    if (newIndex == -1)
      stations.Add(newStation);
    else
      stations.Insert(newIndex, newStation);

    Assert.AreEqual(2, stations.Count);
    Assert.AreEqual(2000, stations[1].SiblingIndex);
  }

  [TestMethod]
  public void TestInsertionAtBeginning()
  {
    var stations = new List<MockStation>
    {
      new MockStation { SiblingIndex = 1000, Name = "A" }
    };

    int newSiblingIndex = 500;
    var newStation = new MockStation { SiblingIndex = newSiblingIndex, Name = "NEW" };

    var newIndex = stations.FindIndex(s => s.SiblingIndex > newSiblingIndex);

    if (newIndex == -1)
      stations.Add(newStation);
    else
      stations.Insert(newIndex, newStation);

    Assert.AreEqual(2, stations.Count);
    Assert.AreEqual(500, stations[0].SiblingIndex);
  }
}