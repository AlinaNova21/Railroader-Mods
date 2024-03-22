using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Definitions
{
  class SerializedProgression
  {
    public Dictionary<string, SerializedSection> Sections { get; set; } = new Dictionary<string, SerializedSection>();

    public SerializedProgression()
    {
    }
    public SerializedProgression(Progression progression)
    {
      Log.Information("Serializing progression {id} {progression}", progression.identifier, progression.name);
      var sections = progression.GetComponentsInChildren<Section>();
      Sections = sections.ToDictionary(s => s.identifier, s => new SerializedSection(s));
    }

    public void ApplyTo(Progression progression, ObjectCache cache)
    {
      var sections = progression.GetComponentsInChildren<Section>()
        .ToDictionary(s => s.identifier, s => s);
      foreach (var pair in Sections)
      {
        var identifier = pair.Key;
        var section = pair.Value;
        Log.Information("Patching section {id}", identifier);
        Section sec;
        if (!sections.TryGetValue(identifier, out sec))
        {
          Log.Information("Adding section {id}", identifier);
          var go = new GameObject(identifier);
          go.transform.SetParent(progression.transform);
          sec = go.AddComponent<Section>();
          sec.identifier = identifier;
          cache.Sections.Add(identifier, sec);
        }
        section.ApplyTo(sec, cache);
      }
    }
  }
}