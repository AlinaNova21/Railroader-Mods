using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using HarmonyLib;
using RLD;
using Serilog;
using Track;
using TransformHandles;
using UI.Common;
using UnityEngine;

namespace MapEditor
{
  class TrackSegmentEditor : MonoBehaviour, IPickable
  {
    public TrackSegment? trackNode;
    public float MaxPickDistance => 100f;

    public int Priority => 1;

    private Handle? handle;

    private Serilog.ILogger logger = Log.ForContext(typeof(TrackSegmentEditor));
    public TooltipInfo TooltipInfo => new TooltipInfo($"Track Segment {trackNode?.id}", this.getTooltipText());

    private Window Window { get; set; }

    private string getTooltipText()
    {
      var sb = new StringBuilder()
        .AppendLine($"ID: {trackNode.id}")
        .AppendLine($"A: {trackNode.a.id}")
        .AppendLine($"B: {trackNode.b.id}")
        .AppendLine($"Length: {trackNode.GetLength()}");
      return sb.ToString();
    }

    private static ObjectTransformGizmo Gizmo
    {
      get
      {
        if (_gizmo == null)
        {
          _gizmo = MonoSingleton<RTGizmosEngine>.Get.CreateObjectUniversalGizmo();
          _gizmo.SetCanAffectScale(false);
          _gizmo.SetTransformSpace(GizmoSpace.Local);
        }
        return _gizmo;
      }
    }
    private static ObjectTransformGizmo? _gizmo;

    public void Activate()
    {
      logger.Information($"Activate {trackNode?.id}");
      if (EditorContext.Instance == null)
      {
        logger.Error("EditorContext.Instance is null");
        return;
      }
      EditorContext.Instance?.SelectSegment(trackNode);
      logger.Information("Set target object");
      // if (handle == null) {
      //   handle = TransformHandleManager.Instance.CreateHandle(trackNode.transform);
      //   handle.OnInteractionEndEvent += e => {
      //     logger.Information("OnInteractionEndEvent");
      //     var nodes = new HashSet<TrackNode>();
      //     var segments = new HashSet<TrackNode>();
      //     if(trackNode == null) {
      //       logger.Error("TrackNode is null");
      //       return;
      //     }

      //     nodes.Add(trackNode);

      //     // This is an attempt to invalidate nearby nodes in the hopes of forcing
      //     // the game to recalculate the track segments and switches
      //     for (var i = 0; i < 2; i++)
      //     {
      //       foreach (var node in nodes.ToArray())
      //       {
      //         var segs2 = Graph.Shared.SegmentsConnectedTo(node);
      //         foreach (var seg in segs2)
      //         {
      //           segments.Add(node);
      //           nodes.Add(seg.a);
      //           nodes.Add(seg.b);
      //         }
      //       }
      //     }
      //     nodes.Do(n => Graph.Shared.OnNodeDidChange(n));
      //   };
      // }
      // handle.ChangeHandleSpace(Space.Self);
      // handle.ChangeHandleType(HandleType.Position | HandleType.Rotation);
      // handle.Enable(trackNode.transform);
      // return;
      // Gizmo.SetTargetPivotObject(this.gameObject);
      Gizmo.SetEnabled(true);
      Gizmo.Settings.SetObjectTransformable(this.gameObject, true);
      ObjectTransformGizmo.ObjectRestrictions restrictions = new ObjectTransformGizmo.ObjectRestrictions();
      restrictions.SetIsAffectedByHandle(GizmoHandleId.CamXYRotation, false);
      restrictions.SetIsAffectedByHandle(GizmoHandleId.CamZRotation, false);
      Gizmo.Gizmo.SetEnabled(true);
      Gizmo.RegisterObjectRestrictions(this.gameObject, restrictions);
      Gizmo.SetTargetObject(this.gameObject);
    }

    public void Deactivate()
    {
    }


    public void Update()
    {
    }

    public void OnDisable()
    {
      if (_gizmo != null)
      {
        _gizmo.Gizmo.SetEnabled(false);
      }
    }

  }
}
