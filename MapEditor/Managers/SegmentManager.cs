using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using AutoTrestle;
using MapEditor.Extensions;
using MapEditor.StateTracker.Segment;
using Newtonsoft.Json.Linq;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapEditor.Managers
{
  public static class SegmentManager
  {

    public static void Move(Direction direction)
    {
      // SelectedNode is already moved by NodeManager
      if (EditorContext.SelectedSegment!.a.id != EditorContext.SelectedNode?.id)
      {
        NodeManager.Move(direction, EditorContext.SelectedSegment.a);
      }

      if (EditorContext.SelectedSegment.b.id != EditorContext.SelectedNode?.id)
      {
        NodeManager.Move(direction, EditorContext.SelectedSegment.b);
      }
    }

    public static void UpdatePriority(int priority, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).Priority(priority));
    }

    public static void UpdateSpeedLimit(int speedLimit, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).SpeedLimit(speedLimit));
    }

    public static void UpdateGroup(string groupId, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).GroupId(groupId));
    }

    public static void UpdateStyle(TrackSegment.Style style, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).Style(style));
      Rebuild();
    }

    public static void UpdateTrackClass(TrackClass trackClass, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).TrackClass(trackClass));
    }

    public static void RemoveSegment(TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new DeleteTrackSegment(segment!));
      Rebuild();
    }

    private static void Rebuild()
    {
      // not sure why this is not working, but calling same method from 'Rebuild Track' button works ...
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }

    public static readonly List<string> TrestleEndStyles = Enum.GetNames(typeof(AutoTrestle.AutoTrestle.EndStyle)).ToList();

    private static readonly Vector3 _TrestleOffset = new(0, -0.35f, 0);

    public static void UpdateTrestle(TrackSegment? segment = null, int? headStyle = null, int? tailStyle = null)
    {
      segment ??= EditorContext.SelectedSegment!;

      var builder = new AutoTrestleBuilder();

      var points = new JArray
      {
        new JObject
        {
          { "position", (segment.a.transform.localPosition + _TrestleOffset).ToJObject() },
          { "rotation", segment.a.transform.eulerAngles.ToJObject() }
        },
        new JObject
        {
          { "position", (segment.b.transform.localPosition + _TrestleOffset).ToJObject() },
          { "rotation", segment.b.transform.eulerAngles.ToJObject() }
        }
      };

      var trestleId = segment.id + "_Trestle";

      EditorContext.PatchEditor!.AddOrUpdateSpliney(trestleId, o =>
      {
        o ??= new JObject
        {
          { "handler", "StrangeCustoms.AutoTrestleBuilder" },
        };

        o["points"] = points;
        
        if (headStyle != null)
        {
          o[headStyle] = TrestleEndStyles[headStyle.Value];
        }
        if (tailStyle != null)
        {
          o[tailStyle] = TrestleEndStyles[tailStyle.Value];
        }

        return o;
      });
      var data = EditorContext.PatchEditor.GetSplineys()[trestleId]!;

      var parentTransform = segment.a.transform.parent;
      var old = parentTransform.GetComponentInChildren<AutoTrestle.AutoTrestle>();
      if (old != null && old.name == trestleId)
      {
        Object.Destroy(old.gameObject);
      }
   
      builder.BuildSpliney(trestleId, parentTransform, data);

    }

  }
}

// copied from SC
namespace StrangeCustoms
{
  internal class AutoTrestleBuilder : ISplineyBuilder
  {
    private AutoTrestleProfile? _Profile;

    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
    {
      if (_Profile == null)
      {
        _Profile = Object.FindObjectOfType<AutoTrestle.AutoTrestle>()!.profile!;
      }

      AutoTrestleData autoTrestleData = data.ToObject<AutoTrestleData>()!;
      if (autoTrestleData?.Points == null || autoTrestleData.Points.Length == 0)
      {
        throw new ArgumentException("No points supplied");
      }

      GameObject gameObject = new GameObject(id);
      gameObject.SetActive(false);
      gameObject.transform.SetParent(parentTransform, false);

      Vector3 center = autoTrestleData.Points.Aggregate(Vector3.zero, (a, b) => a + b.Position) / autoTrestleData.Points.Length;
      gameObject.transform.localPosition = center;

      AutoTrestle.AutoTrestle autoTrestle = gameObject.AddComponent<AutoTrestle.AutoTrestle>();
      autoTrestle.controlPoints = autoTrestleData.Points.Select(s => new AutoTrestle.AutoTrestle.ControlPoint
      {
        position = s.Position - center,
        rotation = Quaternion.Euler(s.Rotation)
      }).ToList();

      autoTrestle.headStyle = autoTrestleData.HeadStyle;
      autoTrestle.tailStyle = autoTrestleData.TailStyle;
      autoTrestle.profile = _Profile;
      gameObject.SetActive(true);
      return gameObject;
    }
  }

  internal class AutoTrestleData
  {
    public SerializedSplinePoint[] Points { get; set; }

    public AutoTrestle.AutoTrestle.EndStyle HeadStyle { get; set; }

    public AutoTrestle.AutoTrestle.EndStyle TailStyle { get; set; }
  }
}
