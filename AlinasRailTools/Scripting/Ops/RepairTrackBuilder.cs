using System.Reflection;
using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class RepairTrackBuilder : BaseIndustryComponentBuilder<RepairTrackBuilder>
{
  public RepairTrack RepairTrack => (RepairTrack)Component;

  public RepairTrackBuilder(IndustryBuilder parent, RepairTrack component)
    : base(parent, component) { }

  // Properties for RepairTrack fields
  public bool CanOverhaul { get => RepairTrack.canOverhaul; set { RepairTrack.canOverhaul = value; Invalidate(); } }

  public Load RepairPartsLoad
  {
    get
    {
      var field = typeof(RepairTrack).GetField("repairPartsLoad", BindingFlags.NonPublic | BindingFlags.Instance);
      return (Load)field?.GetValue(RepairTrack);
    }
    set
    {
      var field = typeof(RepairTrack).GetField("repairPartsLoad", BindingFlags.NonPublic | BindingFlags.Instance);
      field?.SetValue(RepairTrack, value);
      Invalidate();
    }
  }

  // Fluent methods
  public RepairTrackBuilder WithCanOverhaul(bool canOverhaul = true) { CanOverhaul = canOverhaul; return this; }
  public RepairTrackBuilder WithRepairPartsLoad(Load repairPartsLoad) { RepairPartsLoad = repairPartsLoad; return this; }
}