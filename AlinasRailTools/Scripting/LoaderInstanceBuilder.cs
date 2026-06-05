using System;
using System.Linq;
using System.Reflection;
using Model.Ops;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting;

public class LoaderInstanceBuilder : BaseBuilder<LoaderBuilder, LoaderInstanceBuilder>
{
  private readonly Serilog.ILogger logger = Log.ForContext<LoaderInstanceBuilder>();

  public LoaderInstance Loader { get; }
  public Helper Helper => Parent.Helper;

  public LoaderInstanceBuilder(LoaderBuilder parent, LoaderInstance loader)
    : base(parent, loader.identifier)
  {
    Loader = loader;
  }

  public override LoaderBuilder Remove()
  {
    // Clean up prefab instance if it exists
    if (Loader.prefabInstance != null)
    {
      Helper.Prefabs.RemoveInstance(Loader.prefabInstance.Id);
    }
    return Parent.RemoveLoader(Id);
  }

  public override LoaderInstanceBuilder Invalidate()
  {
    // Invalidate doesn't rebuild - transform changes don't need rebuild
    // Only prefab instance or industry changes trigger rebuilds
    return this;
  }

  // Properties - following TrackNodeBuilder pattern (Position/Rotation are local)
  public PrefabInstance PrefabInstance
  {
    get => Loader.prefabInstance;
    set
    {
      Loader.prefabInstance = value;
      RebuildIndustry();
    }
  }

  public Industry Industry
  {
    get => Loader.industry;
    set
    {
      Loader.industry = value;
      RebuildIndustry();
    }
  }

  public Vector3 Position
  {
    get => Loader.transform.localPosition;
    set
    {
      Loader.transform.localPosition = value;
    }
  }

  public Quaternion Rotation
  {
    get => Loader.transform.localRotation;
    set
    {
      Loader.transform.localRotation = value;
    }
  }

  public Vector3 Euler
  {
    get => Loader.transform.localEulerAngles;
    set
    {
      Loader.transform.localEulerAngles = value;
    }
  }

  public Vector3 EulerAngles
  {
    get => Loader.transform.localEulerAngles;
    set
    {
      Loader.transform.localEulerAngles = value;
    }
  }

  private void RebuildIndustry()
  {
    if (PrefabInstance == null || Industry == null)
    {
      return;
    }

    try
    {
      var targetLoader = PrefabInstance.Instance.GetComponentInChildren<CarLoadTargetLoader>();
      if (targetLoader != null) targetLoader.sourceIndustry = Industry;

      var indHover = PrefabInstance.Instance.GetComponentInChildren<IndustryContentHoverable>();
      if (indHover != null)
      {
        indHover.GetType()
          .GetField("industry", BindingFlags.NonPublic | BindingFlags.Instance)
          ?.SetValue(indHover, Industry);
      }
    }
    catch (Exception e)
    {
      logger.Error(e, "Failed to set industry for loader {Loader}", Id);
    }
  }

  // Fluent methods - following TrackNodeBuilder pattern
  public LoaderInstanceBuilder WithPrefabInstance(PrefabInstance prefabInstance)
  {
    PrefabInstance = prefabInstance;
    return this;
  }

  public LoaderInstanceBuilder WithIndustry(Industry industry)
  {
    Industry = industry;
    return this;
  }

  public LoaderInstanceBuilder At(Vector3 position)
  {
    Position = position;
    return this;
  }

  public LoaderInstanceBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));

  public LoaderInstanceBuilder WithRotation(Vector3 rotation)
  {
    Euler = rotation;
    return this;
  }

  public LoaderInstanceBuilder WithRotation(float x, float y, float z) => WithRotation(new Vector3(x, y, z));

  public LoaderInstanceBuilder WithRotation(Quaternion rotation)
  {
    Rotation = rotation;
    return this;
  }

  public LoaderInstanceBuilder WithRotation(float y) => WithRotation(Euler.x, y, Euler.z);

  public override LoaderInstanceBuilder SetActive(bool active = true)
  {
    Loader.gameObject.SetActive(active);
    if (PrefabInstance != null)
    {
      PrefabInstance.SetActive(active);
    }
    return this;
  }
}
