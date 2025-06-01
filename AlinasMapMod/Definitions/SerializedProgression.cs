using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using Game.Progression;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Definitions;

public class SerializedProgression
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

  public void ApplyTo(Progression progression)
  {
    foreach (var pair in Sections) {
      var identifier = pair.Key;
      var section = pair.Value;
      Log.Information("Patching section {id}", identifier);
      if (!SectionCache.Instance.TryGetValue(identifier, out var sec)) {
        Log.Information("Adding section {id}", identifier);
        var go = new GameObject(identifier);
        go.transform.SetParent(progression.transform);
        sec = go.AddComponent<Section>();
        sec.identifier = identifier;
        SectionCache.Instance.Add(identifier, sec);
      }
      try {
        section.ApplyTo(sec);
      } catch (ValidationException e) {
        Log.Error(e, "Error applying section {id}: {message}", identifier, e.Message);
        throw new ValidationException($"Error applying section {identifier}: {e.Message}", e);
      }
    }
  }
}
