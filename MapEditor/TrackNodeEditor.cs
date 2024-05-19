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
  class TrackNodeEditor : MonoBehaviour, IPickable, IRTTransformGizmoListener
  {
    public TrackNode? trackNode;
    public float MaxPickDistance => 100f;

    public int Priority => 1;

    private Handle? handle;

    private Serilog.ILogger logger = Log.ForContext(typeof(TrackNodeEditor));
    public TooltipInfo TooltipInfo => new TooltipInfo($"Track Node {trackNode?.id}", this.getTooltipText());

    private Window Window { get; set; }

    private string getTooltipText()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"ID: {trackNode?.id}");
      sb.AppendLine($"Pos: {trackNode?.transform.localPosition}");
      sb.AppendLine($"Rot: {trackNode?.transform.localEulerAngles}");
      var segments = Graph.Shared.SegmentsConnectedTo(trackNode).Select(s => $"{s.id} ({(s.a == trackNode ? "A" : "B")})");
      sb.AppendLine($"Segments: {String.Join(", ", segments)}");
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
      EditorContext.Instance?.SelectNode(trackNode);
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

    public bool OnCanBeTransformed(Gizmo transformGizmo)
    {
      return true;
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

    public void OnTransformed(Gizmo gizmo)
    {
      if (trackNode == null)
      {
        logger.Error("TrackNode is null");
        return;
      }
      logger.Information($"Transformed {trackNode.id} {gizmo.Transform.Position3D} {gizmo.Transform.Rotation3D}");

      trackNode.transform.position = gizmo.Transform.Position3D;
      trackNode.transform.rotation = gizmo.Transform.Rotation3D;

      gameObject.transform.position = gizmo.Transform.Position3D;
      gameObject.transform.rotation = gizmo.Transform.Rotation3D;

      var nodes = new HashSet<TrackNode>();
      var segments = new HashSet<TrackNode>();

      nodes.Add(trackNode);

      // This is an attempt to invalidate nearby nodes in the hopes of forcing
      // the game to recalculate the track segments and switches
      for (var i = 0; i < 3; i++)
      {
        foreach (var node in nodes.ToArray())
        {
          var segs2 = Graph.Shared.SegmentsConnectedTo(node);
          foreach (var seg in segs2)
          {
            segments.Add(node);
            nodes.Add(seg.a);
            nodes.Add(seg.b);
          }
        }
      }
      nodes.Do(n => Graph.Shared.OnNodeDidChange(n));
    }
  }
}
