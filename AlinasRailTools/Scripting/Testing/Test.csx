using AlinasRailTools.Scripting;
using UnityEngine;

class Test
{
  public void TestFunct()
  {
    var zone = "AN_Whittier_Sawmill_Connection";
    var helper = new Helper(zone);
    var int1 = helper
      .GetTrackNode("N1be")
      .WithFlipSwitchStand(true);
    var sawmill = helper
      .GetTrackNode("N72a");
    var yard00 = helper
      .CreateTrackNode(helper.IDs.Node.Next())
      .At(12915, 561.25f, 4564)
      .WithRotation(0, 312, 0);
    yard00
      .ConnectTo(int1.Node)
      .WithPriority(-1);
    var yard01 = yard00.Extend(120, -1, 0)
      .Extend(120, -1, 0);
    yard01.ConnectTo(sawmill.Node);
    yard01.Rotation *= Quaternion.Euler(0, 1.6f, 0);
    yard01.Position += new Vector3(4, -1.25f, 1);

    
  }
}
