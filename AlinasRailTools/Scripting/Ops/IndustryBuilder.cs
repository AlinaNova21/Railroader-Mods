using System;
using AlinasRailTools.Scripting;
using System.Collections.Generic;
using System.Linq;
using Model.Ops;
using UnityEngine;

namespace AlinasRailTools.Scripting.Ops;

public class IndustryBuilder: BaseBuilder<AreaBuilder, IndustryBuilder>
{
  public Industry Industry { get; }
  public Helper Helper => Parent.Helper;
  public AreaBuilder Area => Parent;

  public Dictionary<string, IndustryComponent> AllComponents = [];
  public Dictionary<string, IndustryComponent> Components = [];

  public Dictionary<string, TeamTrackProfile> AllTeamTrackProfiles = [];
  public Dictionary<string, TeamTrackProfile> TeamTrackProfiles = [];

  public Dictionary<string, PassengerStop> AllPassengerStops = [];
  public Dictionary<string, PassengerStop> PassengerStops = [];

  public IndustryBuilder(AreaBuilder areaBuilder, Industry industry) : base(areaBuilder, industry.identifier) => Industry = industry;

  public override AreaBuilder Remove() => Parent.RemoveIndustry(Id);

  public override IndustryBuilder Invalidate()
  {
    var field = typeof(Industry).GetField("_cachedComponents");
    field?.SetValue(Industry, null); // Invalidate the cache to force a refresh
    return this;
  }

  public bool UsesContract { get => Industry.usesContract; set => Industry.usesContract = value; }
  public Vector3 Position { get => Industry.transform.position; set => Industry.transform.position = value; }
  public string Name { get => Industry.name; set { Industry.name = value; } }

  public IndustryBuilder WithUsesContract(bool usesContract = true) { UsesContract = usesContract; return this; }
  public IndustryBuilder WithName(string name) { Name = name; return this; }
  public IndustryBuilder At(Vector3 position) { Position = position; return this; }
  public IndustryBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));
  public override IndustryBuilder SetActive(bool active = true) { Industry.gameObject.SetActive(active); return this; }

  public void ReloadComponentCache()
  {
    AllComponents = Industry.Components.ToDictionary(c => c.subIdentifier, c => c);
    Components = new Dictionary<string, IndustryComponent>(AllComponents);
  }

  public void ReloadTeamTrackProfileCache()
  {
    // Load all TeamTrackProfile ScriptableObjects from Resources or similar
    var profiles = UnityEngine.Resources.FindObjectsOfTypeAll<TeamTrackProfile>();
    AllTeamTrackProfiles = profiles.ToDictionary(p => p.name, p => p);
    TeamTrackProfiles = new Dictionary<string, TeamTrackProfile>(AllTeamTrackProfiles);
  }

  public void ReloadPassengerStopCache()
  {
    var stops = Industry.GetComponentsInChildren<PassengerStop>();
    AllPassengerStops = stops.ToDictionary(s => s.identifier, s => s);
    PassengerStops = new Dictionary<string, PassengerStop>(AllPassengerStops);
  }

  public T Component<T>(string id) where T : class
  {
    // Try to get existing component first
    var component = Components.Values.FirstOrDefault(c => c.subIdentifier == id);

    // If not found, create based on the requested type
    if (component == null)
    {
      return typeof(T).Name switch
      {
        nameof(FormulaicIndustryComponentBuilder) => CreateFormulaic(id) as T,
        nameof(IndustryLoaderBuilder) => CreateLoader(id) as T,
        nameof(IndustryUnloaderBuilder) => CreateUnloader(id) as T,
        nameof(InterchangeBuilder) => CreateInterchange(id) as T,
        nameof(InterchangedIndustryLoaderBuilder) => CreateInterchangedLoader(id) as T,
        nameof(LoadExporterBuilder) => CreateExporter(id) as T,
        nameof(LoadImporterBuilder) => CreateImporter(id) as T,
        nameof(ProgressionIndustryComponentBuilder) => CreateProgression(id) as T,
        nameof(RepairTrackBuilder) => CreateRepairTrack(id) as T,
        nameof(TeamTrackBuilder) => CreateTeamTrack(id) as T,
        nameof(TeleportLoadingIndustryBuilder) => CreateTeleportLoader(id) as T,
        _ => throw new NotImplementedException($"Component builder for {typeof(T).Name} not implemented")
      };
    }

    // Return builder for existing component
    return typeof(T).Name switch
    {
      nameof(FormulaicIndustryComponentBuilder) when component is FormulaicIndustryComponent fic =>
        new FormulaicIndustryComponentBuilder(this, fic) as T,
      nameof(IndustryLoaderBuilder) when component is IndustryLoader il =>
        new IndustryLoaderBuilder(this, il) as T,
      nameof(IndustryUnloaderBuilder) when component is IndustryUnloader iu =>
        new IndustryUnloaderBuilder(this, iu) as T,
      nameof(InterchangeBuilder) when component is Interchange ic =>
        new InterchangeBuilder(this, ic) as T,
      nameof(InterchangedIndustryLoaderBuilder) when component is InterchangedIndustryLoader iil =>
        new InterchangedIndustryLoaderBuilder(this, iil) as T,
      nameof(LoadExporterBuilder) when component is LoadExporter le =>
        new LoadExporterBuilder(this, le) as T,
      nameof(LoadImporterBuilder) when component is LoadImporter li =>
        new LoadImporterBuilder(this, li) as T,
      nameof(ProgressionIndustryComponentBuilder) when component is ProgressionIndustryComponent pic =>
        new ProgressionIndustryComponentBuilder(this, pic) as T,
      nameof(RepairTrackBuilder) when component is RepairTrack rt =>
        new RepairTrackBuilder(this, rt) as T,
      nameof(TeamTrackBuilder) when component is TeamTrack tt =>
        new TeamTrackBuilder(this, tt) as T,
      nameof(TeleportLoadingIndustryBuilder) when component is TeleportLoadingIndustry tli =>
        new TeleportLoadingIndustryBuilder(this, tli) as T,
      _ => throw new InvalidOperationException($"Component {id} exists but is not compatible with builder type {typeof(T).Name}")
    };
  }

  public FormulaicIndustryComponentBuilder Formulaic(string id)
  {
    return Component<FormulaicIndustryComponentBuilder>(id);
  }

  public IndustryLoaderBuilder Loader(string id)
  {
    return Component<IndustryLoaderBuilder>(id);
  }

  public IndustryUnloaderBuilder Unloader(string id)
  {
    return Component<IndustryUnloaderBuilder>(id);
  }

  public InterchangeBuilder Interchange(string id)
  {
    return Component<InterchangeBuilder>(id);
  }

  public InterchangedIndustryLoaderBuilder InterchangedLoader(string id)
  {
    return Component<InterchangedIndustryLoaderBuilder>(id);
  }

  public LoadExporterBuilder Exporter(string id)
  {
    return Component<LoadExporterBuilder>(id);
  }

  public LoadImporterBuilder Importer(string id)
  {
    return Component<LoadImporterBuilder>(id);
  }

  public ProgressionIndustryComponentBuilder Progression(string id)
  {
    return Component<ProgressionIndustryComponentBuilder>(id);
  }

  public RepairTrackBuilder RepairTrack(string id)
  {
    return Component<RepairTrackBuilder>(id);
  }

  public TeamTrackBuilder TeamTrack(string id)
  {
    return Component<TeamTrackBuilder>(id);
  }

  public TeleportLoadingIndustryBuilder TeleportLoader(string id)
  {
    return Component<TeleportLoadingIndustryBuilder>(id);
  }

  public IndustryBuilder RemoveComponent(string id) {
    if (Components.ContainsKey(id)) {
      Components[id].enabled = false;
      Components.Remove(id);
    }
    return this;
  }

  // Create methods for specific industry component types
  public IndustryBuilder CreateFormulaic(string id, Action<FormulaicIndustryComponentBuilder> action) { action(CreateFormulaic(id)); return this; }
  public FormulaicIndustryComponentBuilder CreateFormulaic(string id)
  {
    FormulaicIndustryComponent component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.FormulaicIndustryComponent) {
      var go = new UnityEngine.GameObject($"Formulaic_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<FormulaicIndustryComponent>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (FormulaicIndustryComponent)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new FormulaicIndustryComponentBuilder(this, component);
  }

  public IndustryBuilder CreateLoader(string id, Action<IndustryLoaderBuilder> action) { action(CreateLoader(id)); return this; }
  public IndustryLoaderBuilder CreateLoader(string id)
  {
    IndustryLoader component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.IndustryLoader) {
      var go = new UnityEngine.GameObject($"Loader_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<IndustryLoader>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (IndustryLoader)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new IndustryLoaderBuilder(this, component);
  }

  public IndustryBuilder CreateUnloader(string id, Action<IndustryUnloaderBuilder> action) { action(CreateUnloader(id)); return this; }
  public IndustryUnloaderBuilder CreateUnloader(string id)
  {
    IndustryUnloader component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.IndustryUnloader) {
      var go = new UnityEngine.GameObject($"Unloader_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<IndustryUnloader>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (IndustryUnloader)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new IndustryUnloaderBuilder(this, component);
  }

  public IndustryBuilder CreateInterchange(string id, Action<InterchangeBuilder> action) { action(CreateInterchange(id)); return this; }
  public InterchangeBuilder CreateInterchange(string id)
  {
    Interchange component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.Interchange) {
      var go = new UnityEngine.GameObject($"Interchange_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<Interchange>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (Interchange)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new InterchangeBuilder(this, component);
  }

  public IndustryBuilder CreateInterchangedLoader(string id, Action<InterchangedIndustryLoaderBuilder> action) { action(CreateInterchangedLoader(id)); return this; }
  public InterchangedIndustryLoaderBuilder CreateInterchangedLoader(string id)
  {
    InterchangedIndustryLoader component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.InterchangedIndustryLoader) {
      var go = new UnityEngine.GameObject($"InterchangedLoader_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<InterchangedIndustryLoader>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (InterchangedIndustryLoader)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new InterchangedIndustryLoaderBuilder(this, component);
  }

  public IndustryBuilder CreateExporter(string id, Action<LoadExporterBuilder> action) { action(CreateExporter(id)); return this; }
  public LoadExporterBuilder CreateExporter(string id)
  {
    LoadExporter component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.LoadExporter) {
      var go = new UnityEngine.GameObject($"Exporter_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<LoadExporter>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (LoadExporter)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new LoadExporterBuilder(this, component);
  }

  public IndustryBuilder CreateImporter(string id, Action<LoadImporterBuilder> action) { action(CreateImporter(id)); return this; }
  public LoadImporterBuilder CreateImporter(string id)
  {
    LoadImporter component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.LoadImporter) {
      var go = new UnityEngine.GameObject($"Importer_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<LoadImporter>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (LoadImporter)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new LoadImporterBuilder(this, component);
  }

  public IndustryBuilder CreateProgression(string id, Action<ProgressionIndustryComponentBuilder> action) { action(CreateProgression(id)); return this; }
  public ProgressionIndustryComponentBuilder CreateProgression(string id)
  {
    ProgressionIndustryComponent component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.ProgressionIndustryComponent) {
      var go = new UnityEngine.GameObject($"Progression_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<ProgressionIndustryComponent>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (ProgressionIndustryComponent)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new ProgressionIndustryComponentBuilder(this, component);
  }

  public IndustryBuilder CreateRepairTrack(string id, Action<RepairTrackBuilder> action) { action(CreateRepairTrack(id)); return this; }
  public RepairTrackBuilder CreateRepairTrack(string id)
  {
    RepairTrack component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.RepairTrack) {
      var go = new UnityEngine.GameObject($"RepairTrack_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<RepairTrack>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (RepairTrack)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new RepairTrackBuilder(this, component);
  }

  public IndustryBuilder CreateTeamTrack(string id, Action<TeamTrackBuilder> action) { action(CreateTeamTrack(id)); return this; }
  public TeamTrackBuilder CreateTeamTrack(string id)
  {
    TeamTrack component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.TeamTrack) {
      var go = new UnityEngine.GameObject($"TeamTrack_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<TeamTrack>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (TeamTrack)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new TeamTrackBuilder(this, component);
  }

  public IndustryBuilder CreateTeleportLoader(string id, Action<TeleportLoadingIndustryBuilder> action) { action(CreateTeleportLoader(id)); return this; }
  public TeleportLoadingIndustryBuilder CreateTeleportLoader(string id)
  {
    TeleportLoadingIndustry component;
    if (!AllComponents.TryGetValue(id, out var existing) || existing is not Model.Ops.TeleportLoadingIndustry) {
      var go = new UnityEngine.GameObject($"TeleportLoader_{id}");
      go.SetActive(false);
      go.transform.SetParent(Industry.transform);
      component = go.AddComponent<TeleportLoadingIndustry>();
      component.subIdentifier = id;
      component.enabled = false;
    } else {
      component = (TeleportLoadingIndustry)existing;
    }
    component.enabled = true;
    AllComponents[id] = component;
    Components[id] = component;
    return new TeleportLoadingIndustryBuilder(this, component);
  }

  public IndustryBuilder GetTeamTrackProfile(string id, Action<TeamTrackProfileBuilder> action) { action(GetTeamTrackProfile(id)); return this; }
  public TeamTrackProfileBuilder GetTeamTrackProfile(string id) => TeamTrackProfiles.ContainsKey(id) ? new TeamTrackProfileBuilder(this, TeamTrackProfiles[id]) : throw new Exception($"TeamTrackProfile with id {id} does not exist");
  public IndustryBuilder CreateTeamTrackProfile(string id, Action<TeamTrackProfileBuilder> action) { action(CreateTeamTrackProfile(id)); return this; }
  public TeamTrackProfileBuilder CreateTeamTrackProfile(string id)
  {
    TeamTrackProfile profile;
    if (!AllTeamTrackProfiles.TryGetValue(id, out profile)) {
      profile = ScriptableObject.CreateInstance<TeamTrackProfile>();
      profile.name = id;
      profile.entries = new List<TeamTrackProfile.Entry>();
    }
    AllTeamTrackProfiles[id] = profile;
    TeamTrackProfiles[id] = profile;
    return new TeamTrackProfileBuilder(this, profile);
  }
  public IndustryBuilder TeamTrackProfile(string id, Action<TeamTrackProfileBuilder> action) { action(GetOrCreateTeamTrackProfile(id)); return this; }
  public TeamTrackProfileBuilder TeamTrackProfile(string id) => GetOrCreateTeamTrackProfile(id);
  public IndustryBuilder GetOrCreateTeamTrackProfile(string id, Action<TeamTrackProfileBuilder> action) { action(GetOrCreateTeamTrackProfile(id)); return this; }
  public TeamTrackProfileBuilder GetOrCreateTeamTrackProfile(string id) => TeamTrackProfiles.ContainsKey(id) ? GetTeamTrackProfile(id) : CreateTeamTrackProfile(id);
  public IndustryBuilder RemoveTeamTrackProfile(string id) {
    if (TeamTrackProfiles.ContainsKey(id)) {
      TeamTrackProfiles.Remove(id);
    }
    return this;
  }

  public IndustryBuilder GetPassengerStop(string id, Action<PassengerStopBuilder> action) { action(GetPassengerStop(id)); return this; }
  public PassengerStopBuilder GetPassengerStop(string id) => PassengerStops.ContainsKey(id) ? new PassengerStopBuilder(this, PassengerStops[id]) : throw new Exception($"PassengerStop with id {id} does not exist");
  public IndustryBuilder CreatePassengerStop(string id, Action<PassengerStopBuilder> action) { action(CreatePassengerStop(id)); return this; }
  public PassengerStopBuilder CreatePassengerStop(string id)
  {
    var gameObject = new UnityEngine.GameObject($"PassengerStop_{id}");
    gameObject.SetActive(false);
    gameObject.transform.SetParent(Industry.transform);
    var stop = gameObject.AddComponent<PassengerStop>();
    stop.identifier = id;
    stop.enabled = false;
    AllPassengerStops[id] = stop;
    PassengerStops[id] = stop;
    return new PassengerStopBuilder(this, stop);
  }
  public IndustryBuilder PassengerStop(string id, Action<PassengerStopBuilder> action) { action(GetOrCreatePassengerStop(id)); return this; }
  public PassengerStopBuilder PassengerStop(string id) => GetOrCreatePassengerStop(id);
  public IndustryBuilder GetOrCreatePassengerStop(string id, Action<PassengerStopBuilder> action) { action(GetOrCreatePassengerStop(id)); return this; }
  public PassengerStopBuilder GetOrCreatePassengerStop(string id) => PassengerStops.ContainsKey(id) ? GetPassengerStop(id) : CreatePassengerStop(id);
  public IndustryBuilder RemovePassengerStop(string id) {
    if (PassengerStops.ContainsKey(id)) {
      var stop = PassengerStops[id];
      stop.enabled = false;
      PassengerStops.Remove(id);
    }
    return this;
  }
}