using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;
using Track;

namespace AlinasRailTools.Scripting.Ops;

public class TeamTrackProfileBuilder : BaseBuilder<IndustryBuilder, TeamTrackProfileBuilder>
{
  public TeamTrackProfile Profile { get; }
  public Helper Helper => Parent.Helper;

  public TeamTrackProfileBuilder(IndustryBuilder industryBuilder, TeamTrackProfile profile) : base(industryBuilder, profile.name) => Profile = profile;

  public override IndustryBuilder Remove() => Parent.RemoveTeamTrackProfile(Id);

  public override TeamTrackProfileBuilder Invalidate() => this; // Profiles don't need invalidation

  // Properties for TeamTrackProfile fields
  public List<TeamTrackProfile.Entry> Entries { get => Profile.entries; set { Profile.entries = value; } }

  // Fluent methods for managing entries
  public TeamTrackProfileBuilder WithEntries(params TeamTrackProfile.Entry[] entries)
  {
    Entries = entries.ToList();
    return this;
  }

  public TeamTrackProfileBuilder AddEntry(string tag, bool export, Load load, float loadingTime, CarTypeFilter carTypeFilter)
  {
    Entries.Add(new TeamTrackProfile.Entry
    {
      tag = tag,
      export = export,
      load = load,
      loadingTime = loadingTime,
      carTypeFilter = carTypeFilter
    });
    return this;
  }

  public TeamTrackProfileBuilder AddEntry(string tag, bool export, Load load, float loadingTime, string carTypeFilter = "")
  {
    return AddEntry(tag, export, load, loadingTime, new CarTypeFilter(carTypeFilter));
  }

  public TeamTrackProfileBuilder ClearEntries()
  {
    Entries.Clear();
    return this;
  }

  public TeamTrackProfileBuilder RemoveEntry(string tag)
  {
    var entry = Entries.FirstOrDefault(e => e.tag == tag);
    if (entry.tag != null) // struct comparison
    {
      Entries.Remove(entry);
    }
    return this;
  }

  public TeamTrackProfileBuilder RemoveEntry(Load load)
  {
    var entry = Entries.FirstOrDefault(e => e.load == load);
    if (entry.load != null)
    {
      Entries.Remove(entry);
    }
    return this;
  }
}