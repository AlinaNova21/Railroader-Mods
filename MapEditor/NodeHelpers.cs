using System.Collections;
using System.Linq;
using HarmonyLib;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor
{
  class NodeHelpers : MonoBehaviour
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

    void StartGizmoSync()
    {
      StopGizmoSync();
      _gizmoSyncTask = StartCoroutine(SyncGizmos());
    }

    void StopGizmoSync()
    {
      if (_gizmoSyncTask != null)
      {
        StopCoroutine(_gizmoSyncTask);
        _gizmoSyncTask = null;
      }
    }

    private IEnumerator SyncGizmos()
    {
      var graph = Graph.Shared;
      LastTrackNodeCount = 0;
      while (true)
      {
        var trackNodes = graph.gameObject.GetComponentsInChildren<TrackNode>();
        logger.Debug($"SyncGizmos() LastTrackNodeCount: {LastTrackNodeCount} trackNodes: {trackNodes.Length}");
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
      var attached = 0;
      logger.Debug("AttachGizmos() trackNodes: {trackNodes}", trackNodes.Length);
      var sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
      foreach (var trackNode in trackNodes)
      {
        var id = trackNode.id;
        var test = GetComponentsInChildren<TrackNodeEditor>().Any(tne => tne.trackNode?.id == id);
        if (!test)
        {
          attached++;
          var target = new GameObject
          {
            name = id,
            layer = LayerMask.NameToLayer("Clickable"),
          };
          target.transform.parent = transform;

          target.transform.localPosition = trackNode.transform.localPosition;
          target.transform.localRotation = trackNode.transform.localRotation;
          target.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);

          target.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
          var renderer = target.AddComponent<MeshRenderer>();
          var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
          material.color = Color.cyan;
          renderer.material = material;
          var lineRenderer = target.AddComponent<LineRenderer>();
          lineRenderer.material = material;
          lineRenderer.startWidth = 0.05f;
          lineRenderer.positionCount = 3;
          lineRenderer.useWorldSpace = false;
          lineRenderer.SetPosition(0, new Vector3(-2, 0, -2));
          lineRenderer.SetPosition(1, new Vector3(0, 0, 3));
          lineRenderer.SetPosition(2, new Vector3(2, 0, -2));

          target.AddComponent<TrackNodeEditor>().trackNode = trackNode;
          target.AddComponent<MeshCollider>().sharedMesh = sharedMesh;
        }
      }
      logger.Debug("AttachGizmos() attached: {attached}", attached);
    }

    internal void Reset()
    {
      logger.Debug("Reset()");
      GetComponentsInChildren<TrackNodeEditor>(true)
        .Do(tne => Destroy(tne.gameObject));
      LastTrackNodeCount = 0;
    }
  }
}