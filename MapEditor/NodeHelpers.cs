using System.Collections;
using System.Linq;
using HarmonyLib;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor
{
  class NodeHelpers : Singleton<NodeHelpers>
  {

    private readonly Serilog.ILogger logger = Log.ForContext<NodeHelpers>();

    private GameObject nodeHelperPrefab { get; }
    private GameObject segmentHelperPrefab { get; }
    private int LastTrackNodeCount = 0;
    private Coroutine? _gizmoSyncTask;
    public NodeHelpers()
    {
      nodeHelperPrefab = CreateNodeHelperPrefab();
      segmentHelperPrefab = CreateSegmentHelperPrefab();
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
          var helper = Instantiate(nodeHelperPrefab, trackNode.transform);
          helper.AddComponent<TrackNodeEditor>().trackNode = trackNode;
        }
      }
      logger.Debug("AttachGizmos() attached: {attached}", attached);
    }

    private GameObject CreateNodeHelperPrefab() {
      var target = new GameObject
      {
        layer = LayerMask.NameToLayer("Clickable"),
      };

      target.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);

      var sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
      target.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
      var renderer = target.AddComponent<MeshRenderer>();
      var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
      {
          color = Color.cyan
      };
      renderer.material = material;
      var lineRenderer = target.AddComponent<LineRenderer>();
      lineRenderer.material = material;
      lineRenderer.startWidth = 0.05f;
      lineRenderer.positionCount = 3;
      lineRenderer.useWorldSpace = false;
      lineRenderer.SetPosition(0, new Vector3(-2, 0, -2));
      lineRenderer.SetPosition(1, new Vector3(0, 0, 3));
      lineRenderer.SetPosition(2, new Vector3(2, 0, -2));
      var cc = target.AddComponent<BoxCollider>();
      cc.size = new Vector3(4f, 4f, 4f);
      // target.AddComponent<MeshCollider>().sharedMesh = sharedMesh;
      return target;
    }

    private GameObject CreateSegmentHelperPrefab() {
      var go = new GameObject();
      var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
      {
        color = Color.cyan
      };
      var lr1 = go.AddComponent<LineRenderer>();
      lr1.material = material;
      lr1.startWidth = 0.05f;
      var lr2 = go.AddComponent<LineRenderer>();
      lr2.material = material;
      lr2.startWidth = 0.05f;
      return go;
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