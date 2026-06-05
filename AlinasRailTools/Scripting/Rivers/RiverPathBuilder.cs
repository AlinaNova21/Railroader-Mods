using System.Collections.Generic;
using System.Linq;
using Helpers;
using Map.Runtime.MaskComponents;
using UnityEngine;

namespace AlinasRailTools.Scripting.Rivers;

public class RiverPathBuilder : BaseBuilder<RiversBuilder, RiverPathBuilder>
{
  public RiverPath Path { get; }
  public RiverBuilder Builder { get; }
  public Helper Helper => Parent.Helper;

  private List<RiverPath.Point> points = new();

  public RiverPathBuilder(RiversBuilder riversBuilder, GameObject gameObject)
    : base(riversBuilder, gameObject.name)
  {
    Path = gameObject.GetComponent<RiverPath>();
    Builder = gameObject.GetComponent<RiverBuilder>();

    if (Path == null)
    {
      Path = gameObject.AddComponent<RiverPath>();
    }
    if (Builder == null)
    {
      Builder = gameObject.AddComponent<RiverBuilder>();
    }
  }

  public override RiversBuilder Remove() => Parent.RemoveRiver(Id);

  public override RiverPathBuilder Invalidate() => this;

  // Properties
  public RiverPath.RiverPathStyle Style { get => Path.style; set => Path.style = value; }
  public float YOffset { get => Path.yOffset; set => Path.yOffset = value; }

  // Fluent methods
  public RiverPathBuilder WithProfile(SplineProfile profile)
  {
    // Use reflection to set private splineProfile field
    var profileField = typeof(RiverBuilder).GetField("splineProfile",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (profileField != null)
    {
      profileField.SetValue(Builder, profile);
    }
    return this;
  }

  public RiverPathBuilder WithProfile(string profileName)
  {
    if (Helper.SplineProfiles.TryGetValue(profileName, out var profile))
    {
      return WithProfile(profile);
    }
    else
    {
      Debug.LogWarning($"SplineProfile '{profileName}' not found in cache");
    }
    return this;
  }

  public RiverPathBuilder WithStyle(RiverPath.RiverPathStyle style)
  {
    Style = style;
    return this;
  }

  public RiverPathBuilder WithYOffset(float yOffset)
  {
    YOffset = yOffset;
    return this;
  }

  public RiverPathBuilder AddPoint(Vector3 position, Vector3 eulerAngles, float width)
  {
    points.Add(new RiverPath.Point
    {
      position = position,
      eulerAngles = eulerAngles,
      width = width
    });
    return this;
  }

  public RiverPathBuilder ClearPoints()
  {
    points.Clear();
    return this;
  }

  public RiverPathBuilder SetPoints(RiverPath.Point[] newPoints)
  {
    points = new List<RiverPath.Point>(newPoints);
    return this;
  }

  public override RiverPathBuilder SetActive(bool active)
  {
    if (active && points.Count > 0)
    {
      // Calculate center as average of all positions
      var center = Vector3.zero;
      foreach (var point in points)
      {
        center += point.position;
      }
      center /= points.Count;

      // Set GameObject position to center
      Path.gameObject.transform.position = center;

      // Convert all points to offsets from center
      var offsetPoints = new List<RiverPath.Point>();
      for (int i = 0; i < points.Count; i++)
      {
        offsetPoints.Add(new RiverPath.Point
        {
          position = points[i].position - center,
          eulerAngles = points[i].eulerAngles,
          width = points[i].width
        });
      }

      // Assign to Path.points
      Path.points = offsetPoints;
    }

    return base.SetActive(active);
  }
}
