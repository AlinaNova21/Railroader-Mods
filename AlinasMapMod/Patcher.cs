using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using AlinasMapMod.Definitions;
using Game.Messages.OpsSnapshot;
using Game.Progression;
using JetBrains.Annotations;
using Model.Ops;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Railloader;
using Serilog;
using UnityEngine;

namespace AlinasMapMod
{
  public class Patcher
  {
    private PatchState _initialState;
    public PatchState InitialState {
      get {
        if (_initialState == null) {
          _initialState = Dump();
        }
        return _initialState;
      }
    }

    public delegate void PatchStateHandler(PatchState state);
    public event PatchStateHandler OnPatchState;
    protected virtual void OnPatchStateEvent(PatchState state) {
      OnPatchState?.Invoke(state);
    }

    public void Patch() {
      var cache = new ObjectCache();
      cache.Rebuild();
      var mapFeatures = UnityEngine.Object.FindObjectsOfType<MapFeature>(false)
        .ToDictionary(mf => mf.identifier, mf => mf);
      var mapFeatureManager = UnityEngine.Object.FindObjectOfType<MapFeatureManager>(false);
      var progressions = UnityEngine.Object.FindObjectsOfType<Progression>(false)
        .ToDictionary(p => p.identifier, p => p);

      foreach(var mixinto in AlinasMapMod.Shared.GetMixintos("progressions")) {
        var json = JObject.Parse(File.ReadAllText(mixinto.Mixinto));
        var state = json.ToObject<PatchState>();
        if (state == null) continue;
        OnPatchStateEvent(state);
        foreach (KeyValuePair<string, SerializedMapFeature> pair in state.MapFeatures)
        {
          var identifier = pair.Key;
          var mapFeature = pair.Value;

          if (mapFeature == null) {
            Log.Warning("MapFeatures cannot be deleted. {id}", identifier);
            continue;
          }
          MapFeature existing;
          if (!mapFeatures.TryGetValue(identifier, out existing))
          {
            Log.Warning("Creating MapFeature {id}", identifier);
            var go = new GameObject(identifier);
            go.transform.SetParent(mapFeatureManager.transform);
            existing = go.AddComponent<MapFeature>();
            existing.identifier = identifier;
            cache.MapFeatures.Add(identifier, existing);
          }
          Log.Information("Patching MapFeature {id}", identifier);
          mapFeature.ApplyTo(existing, cache);
        }

        foreach (KeyValuePair<string, SerializedProgression> pair in state.Progressions)
        {
          var identifier = pair.Key;
          var progression = pair.Value;

          if (progression == null) {
            Log.Warning("Progressions cannot be deleted. {id}", identifier);
            continue;
          }

          if (progressions.TryGetValue(identifier, out var existing))
          {
            Log.Information("Patching progression {id}", identifier);
            progression.ApplyTo(existing, cache);
          }
          else
          {
            Log.Warning("Progression missing {id}", identifier);
            var progressionsObj = GameObject.Find("Progressions");
            var go = new GameObject(identifier);
            go.transform.SetParent(progressionsObj.transform);
            var comp = go.AddComponent<Progression>();
            comp.identifier = identifier;
            comp.mapFeatureManager = mapFeatureManager;
            go.SetActive(true);
            progression.ApplyTo(comp, cache);
          }
        }
      }
    }
    public PatchState Dump(string path = "")
    {
      try
      {
        var state = new PatchState
        {
          Progressions = UnityEngine.Object.FindObjectsByType<Progression>(UnityEngine.FindObjectsSortMode.None)
              .ToDictionary(p => p.identifier, p => new SerializedProgression(p)),
          MapFeatures = UnityEngine.Object.FindObjectsByType<MapFeature>(UnityEngine.FindObjectsSortMode.None)
              .ToDictionary(mf => mf.identifier, mf => new SerializedMapFeature(mf)),
        };
        if (path != "")
        {
          var jsonSerializerSettings = new JsonSerializerSettings
          {
            ContractResolver = new DefaultContractResolver
            {
              NamingStrategy = new CamelCaseNamingStrategy
              {
                ProcessDictionaryKeys = false
              }
            },
          };
          var jsonSerializer = JsonSerializer.CreateDefault(jsonSerializerSettings);
          var obj = JObject.FromObject(state, jsonSerializer);
          File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented, jsonSerializerSettings));
        }
        return state;
      }
      catch (System.Exception e)
      {
        Log.Error(e, "Failed to dump progression");
      }
      return new PatchState();
    }
  }
}
