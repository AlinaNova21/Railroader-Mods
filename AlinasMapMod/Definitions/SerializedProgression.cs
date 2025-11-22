using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using AlinasMapMod.Validation;
using Game.Progression;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Definitions;

public class SerializedProgression : SerializedComponentBase<Progression>,
  ICreatableComponent<Progression>,
  IDestroyableComponent<Progression>
{
  public Dictionary<string, SerializedSection> Sections { get; set; } = new Dictionary<string, SerializedSection>();

  public SerializedProgression()
  {
  }
  public SerializedProgression(Progression progression)
  {
    Read(progression);
  }

  protected override void ConfigureValidation()
  {
    // Validate that we have at least one section (warning only)
    RuleFor(() => Sections)
      .Custom((sections, context) =>
      {
        var result = new ValidationResult { IsValid = true };
        if (sections?.Count == 0)
        {
          // This is a warning, not an error - progression can exist without sections initially
          result.Warnings.Add(new ValidationWarning
          {
            Field = nameof(Sections),
            Message = "Progression has no sections defined",
            Value = sections
          });
        }
        return result;
      });

    // Validate section keys are not empty using Custom validation
    RuleFor(() => Sections)
      .Custom((sections, context) =>
      {
        var result = new ValidationResult { IsValid = true };
        
        if (sections != null)
        {
          foreach (var kvp in sections)
          {
            if (string.IsNullOrEmpty(kvp.Key))
            {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                Field = $"{nameof(Sections)}[{kvp.Key}]",
                Message = "Section key cannot be null or empty",
                Code = "REQUIRED",
                Value = kvp.Key
              });
            }
          }
        }
        
        return result;
      });

    // Note: Section validation will be handled when Write is called
  }

  public override Progression Create(string id)
  {
    // Find the Progressions parent GameObject (based on OldPatcher pattern)
    var progressionsObj = GameObject.Find("Progressions");
    if (progressionsObj == null)
    {
      throw new System.InvalidOperationException("Progressions GameObject not found");
    }

    // Create GameObject and Progression component
    var go = new GameObject(id);
    go.transform.SetParent(progressionsObj.transform);
    var progression = go.AddComponent<Progression>();
    progression.identifier = id;
    
    // Set up MapFeatureManager reference (from OldPatcher pattern)
    var mapFeatureManager = Object.FindObjectOfType<MapFeatureManager>(false);
    if (mapFeatureManager != null)
    {
      progression.mapFeatureManager = mapFeatureManager;
    }
    
    go.SetActive(true);
    
    // Register in cache
    ProgressionCache.Instance[id] = progression;
    
    // Apply configuration
    Write(progression);
    
    return progression;
  }

  public override void Write(Progression progression)
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
      section.Write(sec); // Use new Write method
    }
  }

  public override void Read(Progression progression)
  {
    Log.Information("Serializing progression {id} {progression}", progression.identifier, progression.name);
    var sections = progression.GetComponentsInChildren<Section>();
    Sections = sections.ToDictionary(s => s.identifier, s => new SerializedSection(s));
  }

  public void Destroy(Progression progression)
  {
    // Remove from cache
    ProgressionCache.Instance.Remove(progression.identifier);
    
    // Destroy GameObject (this will also destroy child Section components)
    GameObject.Destroy(progression.gameObject);
  }

  public void ApplyTo(Progression progression)
  {
    // Validate before applying
    Validate();
    Write(progression);
  }
}
