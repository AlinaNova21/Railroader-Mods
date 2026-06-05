using System;
using AlinasRailTools.Scripting;
using System.Collections.Generic;
using System.Linq;
using Model.Ops;
using UnityEngine;
using AlinasRailTools.Definitions;
using Track;

namespace AlinasRailTools.Scripting.Ops;

public class AreaBuilder: BaseBuilder<OpsBuilder, AreaBuilder>
{
  public Area Area { get; }
  public Helper Helper => Parent.Helper;

  public Dictionary<string, Industry> AllIndustries = [];
  public Dictionary<string, Industry> Industries = [];

  public AreaBuilder(OpsBuilder opsBuilder, Area area) : base(opsBuilder, area.identifier) => Area = area;

  public override OpsBuilder Remove() => Parent.RemoveArea(Id);

  public override AreaBuilder Invalidate() => this; // Areas don't need graph invalidation like track objects

  public float Radius { get => Area.radius; set { Area.radius = value; } }
  public Color TagColor { get => Area.tagColor; set { Area.tagColor = value; } }
  public Vector3 Position { get => Area.transform.position; set { Area.transform.position = value; } }
  public string Name { get => Area.name; set { Area.name = value; } }

  public void ReloadCaches()
  {
    AllIndustries = Area.Industries.ToDictionary(i => i.identifier, i => i);
    Industries = AllIndustries
      .Where(i => i.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);
    
  }

  public AreaBuilder WithRadius(float radius) { Radius = radius; return this; }
  public AreaBuilder WithTagColor(Color color) { TagColor = color; return this; }
  public AreaBuilder WithTagColor(float r, float g, float b, float a = 1f) => WithTagColor(new Color(r, g, b, a));
  public AreaBuilder WithName(string name) { Name = name; return this; }
  public AreaBuilder At(Vector3 position) { Position = position; return this; }
  public AreaBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));
  public override AreaBuilder SetActive(bool active = true) { Area.gameObject.SetActive(active); return this; }

  public AreaBuilder GetIndustry(string id, Action<IndustryBuilder> action) { action(GetIndustry(id)); return this; }
  public IndustryBuilder GetIndustry(string id) => Industries.ContainsKey(id) ? new IndustryBuilder(this, Industries[id]) : throw new Exception($"Industry with id {id} does not exist in area {Area.identifier}");
  public AreaBuilder CreateIndustry(string id, Action<IndustryBuilder> action) { action(CreateIndustry(id)); return this; }
  public IndustryBuilder CreateIndustry(string id)
  {
    Industry industry;
    if (!AllIndustries.TryGetValue(id, out industry)) {
      var go = new GameObject($"Industry {id}");
      go.SetActive(false);
      industry = go.AddComponent<Industry>();
      industry.identifier = id;
      industry.enabled = false;
    }
    industry.transform.SetParent(Area.transform);
    industry.enabled = true;
    AllIndustries[id] = industry;
    Industries[id] = industry;
    return new IndustryBuilder(this, industry);
  }
  public AreaBuilder Industry(string id, Action<IndustryBuilder> action) { action(GetOrCreateIndustry(id)); return this; }
  public IndustryBuilder Industry(string id) => GetOrCreateIndustry(id);
  public AreaBuilder GetOrCreateIndustry(string id, Action<IndustryBuilder> action) { action(GetOrCreateIndustry(id)); return this; }
  public IndustryBuilder GetOrCreateIndustry(string id) => Industries.ContainsKey(id) ? GetIndustry(id) : CreateIndustry(id);
  public AreaBuilder RemoveIndustry(string id) {
    if (Industries.ContainsKey(id)) {
      Industries[id].enabled = false;
      Industries.Remove(id);
    }
    return this;
  }
}