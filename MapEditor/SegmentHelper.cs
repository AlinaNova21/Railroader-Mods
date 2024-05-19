using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using Track;
using UnityEngine;

namespace MapEditor
{
  class SegmentHelper : MonoBehaviour, IPickable
  {
    private static Material _lineMaterial;
    private static Material LineMaterial
    {
      get
      {
        if (_lineMaterial == null)
        {
          _lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
          {
            color = Color.yellow
          };
        }
        return _lineMaterial;
      }
    }

    private TrackSegment _segment;
    private TrackSegment Segment
    {
      get
      {
        if (_segment == null)
        {
          _segment = transform.parent.GetComponent<TrackSegment>();
        }
        return _segment;
      }
    }

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public float pointAvg = 0;

    public TooltipInfo TooltipInfo => new TooltipInfo($"Segment {Segment.id}", this.getTooltipText());

    private string getTooltipText()
    {
      var sb = new StringBuilder()
        .AppendLine($"ID: {Segment.id}")
        .AppendLine($"A: {Segment.a.id}")
        .AppendLine($"B: {Segment.b.id}")
        .AppendLine($"Length: {Segment.GetLength()}");
      return sb.ToString();
    }

    public void Start()
    {
      gameObject.layer = Layers.Clickable;
    }

    public void Rebuild()
    {
      var lr = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
      var mc = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
      var mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
      var mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
      lr.material = LineMaterial;
      var approx = Segment.Curve.Approximate();
      var points = approx
        .Select(p => p.point)
        .Select(p => p += new Vector3(0, -0.02f, 0))
        .ToArray();

      var avg = points.Aggregate(Vector3.zero, (acc, p) => acc + p) / points.Length;
      var newPointAvg = avg.x + avg.y + avg.z;
      if ((int)(pointAvg * 100) != (int)(newPointAvg * 100))
      {
        pointAvg = newPointAvg;
        lr.useWorldSpace = false;
        var mesh = ExtrudeAlongPath(points, 0.1f);
        mc.sharedMesh = mesh;
        mf.sharedMesh = mesh;
        mr.material = LineMaterial;
        lr.startWidth = 1f;
        lr.startWidth = 0.05f;
      }
    }

    private static Mesh ExtrudeAlongPath(Vector3[] points, float width)
    {
      if (points.Length < 2)
        return null;
      Mesh m = new Mesh();
      List<Vector3> verts = new List<Vector3>();
      List<Vector3> norms = new List<Vector3>();

      for (int i = 0; i < points.Length; i++)
      {
        if (i != points.Length - 1)
        {
          Vector3 perpendicularDirection = new Vector3(-(points[i + 1].z - points[i].z), 0, points[i + 1].x - points[i].x).normalized;
          verts.Add(points[i] + perpendicularDirection * width);
          norms.Add(Vector3.up);
          verts.Add(points[i] + perpendicularDirection * -width);
          norms.Add(Vector3.up);
        }
        else
        {
          Vector3 perpendicularDirection = new Vector3(-(points[i].z - points[i - 1].z), 0, points[i].x - points[i - 1].x).normalized;
          verts.Add(points[i] + perpendicularDirection * -width);
          norms.Add(Vector3.up);
          verts.Add(points[i] + perpendicularDirection * width);
          norms.Add(Vector3.up);
        }
      }
      m.vertices = verts.ToArray();
      m.normals = norms.ToArray();

      List<int> tris = new List<int>();
      for (int i = 0; i < m.vertices.Length - 3; i++)
      {
        if (i % 2 == 0)
        {
          tris.Add(i + 2);
          tris.Add(i + 1);
          tris.Add(i);
        }
        else
        {
          tris.Add(i);
          tris.Add(i + 1);
          tris.Add(i + 2);
        }
      }
      m.triangles = tris.ToArray();

      m.name = "pathMesh";
      m.RecalculateNormals();
      m.RecalculateBounds();
      m.Optimize();
      return m;
    }

    public void Update()
    {
      LineRenderer lr = GetComponent<LineRenderer>();
      MeshRenderer mr = GetComponent<MeshRenderer>();
      if (lr != null && mr != null)
      {
        lr.enabled = EditorMod.Shared.Settings.ShowHelpers;
        mr.enabled = EditorMod.Shared.Settings.ShowHelpers;
      }
      if (!EditorMod.Shared.IsEnabled)
      {
        Destroy(this);
      }
    }

    public void Activate()
    {
    }

    public void Deactivate()
    {
    }
  }
}
