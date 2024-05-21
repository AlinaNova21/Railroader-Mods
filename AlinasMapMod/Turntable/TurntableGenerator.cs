using System.Collections.Generic;
using HarmonyLib;
using Helpers;
using KeyValue.Runtime;
using Newtonsoft.Json.Linq;
using RollingStock.Controls;
using Track;
using UnityEngine;

namespace AlinasMapMod.Turntable
{
  public static class TurntableGenerator
  {
    private static Dictionary<string, GameObject> prefabs { get; set; } = new Dictionary<string, GameObject>();

    public static GameObject Generate(string Namespace, int RoundhouseStalls, Vector3 position = new Vector3(), Vector3 rotation = new Vector3())
    {
      var go = new GameObject(Namespace);
      go.SetActive(false);
      go.transform.localPosition = position;
      go.transform.localEulerAngles = rotation;
      prefabs.Clear();
      var tt = GameObject.Find("30m Turntable");
      prefabs.Add("turntable", tt);
      var rh = GameObject.Find("Roundhouse");
      prefabs.Add("side", rh.transform.Find("Roundhouse Modular A Side").gameObject);
      prefabs.Add("between", rh.transform.Find("Roundhouse Modular A Between").gameObject);
      prefabs.Add("floor", rh.transform.Find("Roundhouse Modular A Floor").gameObject);
      prefabs.Add("stall", rh.transform.Find("Stall").gameObject);
      go.transform.DestroyAllChildren();

      GenerateTurntable(Namespace, RoundhouseStalls, position, rotation).transform.parent = go.transform;
      if (RoundhouseStalls > 0)
      {
        var rhgo = GenerateRoundhouse(Namespace, RoundhouseStalls);
        rhgo.transform.SetParent(go.transform, false);
      }
      go.SetActive(true);
      return go;
    }

    private static GameObject GenerateTurntable(string Namespace, int RoundhouseStalls, Vector3 position, Vector3 rotation)
    {
      var gameObject = new GameObject("Turntable");
      gameObject.SetActive(false);
      var tt = gameObject.AddComponent<Track.Turntable>();
      tt.transform.localPosition = position;
      tt.transform.localEulerAngles = rotation;
      tt.id = Namespace + ".turntable";
      tt.radius = 15;
      tt.subdivisions = 32;

      var nodes = new List<TrackNode>();
      var nodesField = AccessTools.Field(typeof(Track.Turntable), "nodes");
      nodesField.SetValue(tt, nodes);

      for (var i = 0; i < 32; i++)
      {
        var num = tt.AngleForIndex(i);
        Quaternion quaternion = Quaternion.Euler(0f, rotation.y + num, 0f);
        var go = new GameObject();
        var node = go.AddComponent<TrackNode>();
        go.transform.parent = Graph.Shared.transform;
        node.transform.localPosition = position + quaternion * Vector3.forward * tt.radius;
        node.transform.localEulerAngles = new Vector3(0, rotation.y + num, 0);
        node.id = Namespace + ".turntable.node." + i;
        node.name = node.id;
        node.turntable = tt;
        nodes.Add(node);
      }

      for (var i = 1; i <= RoundhouseStalls; i++)
      {
        var dist = 60;
        var num = tt.AngleForIndex(i);
        Quaternion quaternion = tt.transform.rotation * Quaternion.Euler(0f, num, 0f);
        var go = new GameObject();
        var node = go.AddComponent<TrackNode>();
        go.transform.parent = Graph.Shared.transform;
        node.transform.localPosition = position + quaternion * Vector3.forward * dist;
        node.transform.localEulerAngles = new Vector3(0, rotation.y + num, 0);
        node.id = Namespace + ".roundhouse.node." + i;
        node.name = node.id;
        var segment = go.AddComponent<TrackSegment>();
        segment.id = Namespace + ".roundhouse.segment." + i;
        segment.a = nodes[i];
        segment.b = node;
        segment.style = TrackSegment.Style.Yard;
      }

      var ttInstance = GameObject.Instantiate(prefabs["turntable"], gameObject.transform);
      ttInstance.GetComponent<GlobalKeyValueObject>().globalObjectId = Namespace + ".turntable";
      ttInstance.GetComponent<TurntableController>().turntable = tt;
      gameObject.SetActive(true);
      return gameObject;
    }

    private static GameObject GenerateRoundhouse(string Namespace, int RoundhouseStalls)
    {
      var interval = 360 / 32f;
      var rh = new GameObject("Roundhouse"); ;
      rh.SetActive(false);
      rh.transform.localPosition = new Vector3(0, -0.48f, 0);
      rh.transform.localEulerAngles = new Vector3(0, 0, 0);
      rh.transform.localScale = new Vector3(1, 1, 1);
      var rhkv = rh.AddComponent<KeyValueObject>();
      var rhgkv = rh.AddComponent<GlobalKeyValueObject>();

      rhgkv.globalObjectId = Namespace + ".roundhouse";

      var stallCount = RoundhouseStalls;

      var side1 = GameObject.Instantiate(prefabs["side"], rh.transform);
      side1.transform.localEulerAngles = new Vector3(0, 180 + interval, 0);
      side1.transform.localScale = new Vector3(-1, 1, 1);

      var side2 = GameObject.Instantiate(prefabs["side"], rh.transform);
      side2.transform.localEulerAngles = new Vector3(0, (interval * stallCount) + 180, 0);

      for (var i = 0; i < stallCount; i++)
      {
        var angle = (i + 1) * interval;
        var stallInstance = GameObject.Instantiate(prefabs["stall"], rh.transform);
        stallInstance.transform.localEulerAngles = new Vector3(0, angle, 0);
        var kvt = stallInstance.GetComponentInChildren<KeyValuePickableToggle>();
        var kva = stallInstance.GetComponentInChildren<KeyValueBoolAnimator>();
        kvt.key = $"stall-doors.${i}";
        kva.key = $"stall-doors.${i}";
      }
      for (var i = 1; i < stallCount; i++)
      {
        var angle = (i + 1) * interval;
        var betweenInstance = GameObject.Instantiate(prefabs["between"], rh.transform);
        betweenInstance.layer = rh.layer;
        betweenInstance.transform.localEulerAngles = new Vector3(270, angle + 180 - (interval / 2f), 0);
      }
      rh.SetActive(true);
      return rh;
    }

    public static JObject DumpTree(Transform transform)
    {
      var ser = new Newtonsoft.Json.JsonSerializer()
      {
        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
        Formatting = Newtonsoft.Json.Formatting.Indented,
        MaxDepth = 1,
      };
      var obj = new JObject
      {
          { "name", transform.name },
          { "position", JToken.FromObject(transform.localPosition, ser) },
          { "rotation", JToken.FromObject(transform.localEulerAngles, ser) },
          { "scale", JToken.FromObject(transform.localScale, ser) }
      };
      var components = new JArray();
      foreach (var component in transform.GetComponents<Component>())
      {
        components.Add(component.GetType().Name);
      }
      obj.Add("components", components);
      var children = new JArray();
      foreach (Transform child in transform)
      {
        children.Add(DumpTree(child));
      }
      obj.Add("children", children);
      return obj;
    }
  }
}
