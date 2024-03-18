using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Runtime.CompilerServices;
using RLD;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasMapMod.Editor
{
  class NodeHelpers: MonoBehaviour
  {

    private readonly Serilog.ILogger logger = Log.ForContext<NodeHelpers>();

    private int LastTrackNodeCount = 0;
    private Coroutine? _gizmoSyncTask;
    public NodeHelpers()
    {
    }

    public void OnEnable()
    {
      logger.Debug("NodeHelpers OnEnable()");
      StartGizmoSync();
    }

    public void OnDisable()
    {
      logger.Debug("NodeHelpers OnDisable()");
      StopGizmoSync();
    }

    void StartGizmoSync() {
      StopGizmoSync();
      _gizmoSyncTask = StartCoroutine(SyncGizmos());
    }

    void StopGizmoSync() {
      if (_gizmoSyncTask != null) {
        StopCoroutine(_gizmoSyncTask);
        _gizmoSyncTask = null;
      }
    }

    private IEnumerator SyncGizmos() {
      var graph = Graph.Shared;
      LastTrackNodeCount = 0;
      while (true) {
        logger.Debug("SyncGizmos() LastTrackNodeCount: {LastTrackNodeCount}");
        var trackNodes = graph.gameObject.GetComponentsInChildren<TrackNode>();
        if (trackNodes.Length != LastTrackNodeCount)
        {
          LastTrackNodeCount = trackNodes.Length;
          AttachGizmos(trackNodes);
        }
        yield return new WaitForSeconds(1);
      }
    }

    public void AttachGizmos(TrackNode[] trackNodes)
    {
      logger.Debug("AttachGizmos() trackNodes: {trackNodes}", trackNodes.Length);
      var sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
      foreach (var trackNode in trackNodes) {
        var id = trackNode.id;
        var test = GetComponentsInChildren<TrackNodeEditor>().Any(tne => tne.trackNode?.id == id);
        if (!test)
        {
          var target = new GameObject
          {
            name = id,
            layer = LayerMask.NameToLayer("Clickable"),
          };
          target.transform.parent = transform;

          target.transform.position = trackNode.transform.position;
          target.transform.rotation = trackNode.transform.rotation;
          target.transform.localScale = new Vector3(0.1f, 0.1f, 0.3f);

          target.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
          target.AddComponent<MeshRenderer>();
          target.AddComponent<TrackNodeEditor>().trackNode = trackNode;
          target.AddComponent<MeshCollider>().sharedMesh = sharedMesh;
        }
      }
    }
  }
}