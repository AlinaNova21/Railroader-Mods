using AlinasRailTools.Scripting;
using Model.Ops;

namespace AlinasRailTools.Scripting.Ops;

public class TeamTrackBuilder : BaseIndustryComponentBuilder<TeamTrackBuilder>
{
  public TeamTrack TeamTrack => (TeamTrack)Component;

  public TeamTrackBuilder(IndustryBuilder parent, TeamTrack component)
    : base(parent, component) { }

  // Properties for TeamTrack fields
  public TeamTrackProfile Profile { get => TeamTrack.profile; set { TeamTrack.profile = value; Invalidate(); } }
  public float IdealCars { get => TeamTrack.idealCars; set { TeamTrack.idealCars = value; Invalidate(); } }

  // Fluent methods
  public TeamTrackBuilder WithProfile(TeamTrackProfile profile) { Profile = profile; return this; }
  public TeamTrackBuilder WithIdealCars(float idealCars) { IdealCars = idealCars; return this; }
}