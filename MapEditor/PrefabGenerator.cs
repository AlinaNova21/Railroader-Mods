using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Messages.OpsSnapshot;
using JetBrains.Annotations;
using UnityEngine;

namespace TransformHandles
{
  static class PrefabGenerator {

    private static AssetBundle bundle;

    static PrefabGenerator()
    {
      var rth = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapEditor.Resources.rth.runtime");
      var ms = new System.IO.MemoryStream();
      rth.CopyTo(ms);
      byte[] bytes = ms.ToArray();
      AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(bytes);
      // yield return createRequest;
      createRequest.completed += (_) => bundle = createRequest.assetBundle;
    }

    public static GameObject GhostPrefab() {
      return new GameObject("Ghost", typeof(Ghost));  
    }

    public static GameObject TransformHandlePrefab() {
      var coneMesh = bundle.LoadAsset<Mesh>("Assets/RTH.Runtime/Models/cone.asset");
      var tubeMesh = bundle.LoadAsset<Mesh>("Assets/RTH.Runtime/Models/tube.asset");
      var redMaterial = bundle.LoadAsset<Material>("Assets/RTH.Runtime/Material/Red.mat");
      var greenMaterial = bundle.LoadAsset<Material>("Assets/RTH.Runtime/Material/Green.mat");
      var blueMaterial = bundle.LoadAsset<Material>("Assets/RTH.Runtime/Material/Blue.mat");

      var go = new GameObject("TransformHandle");
      var handle = go.AddComponent<Handle>();
      handle.space = Space.Self;
      var lineRotations = new Dictionary<string, Vector3> {
        {"X", Vector3.right},
        {"Y", Vector3.up},
        {"Z", Vector3.forward}
      };
      var coneRotations = new Dictionary<string, Vector3> {
        {"X", Vector3.right},
        {"Y", Vector3.up},
        {"Z", Vector3.forward}
      };
      var colors = new Dictionary<string, Color> {
        {"X", Color.red},
        {"Y", Color.green},
        {"Z", Color.blue}
      };
      var materials = new Dictionary<string, Material> {
        {"X", redMaterial},
        {"Y", greenMaterial},
        {"Z", blueMaterial}
      };
      var configureMR = (Material mat, MeshRenderer mr) =>
      {
        mr.materials = new Material[] { mat };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        mr.allowOcclusionWhenDynamic = true;
        mr.renderingLayerMask = (uint)LayerMask.NameToLayer("Light Layer default");
      };
      var position = new GameObject("Position");
      
      foreach(var axis in new string[] { "X", "Y", "Z" }) {
        var axisGo = new GameObject(axis);
        axisGo.transform.SetParent(position.transform);
        {
          var cone = new GameObject("Cone");
          cone.transform.parent = axisGo.transform;
          var mf = cone.AddComponent<MeshFilter>();
          mf.mesh = coneMesh;
          var mr = cone.AddComponent<MeshRenderer>();
          configureMR(materials[axis], mr);
          var mc = cone.AddComponent<MeshCollider>();
          mc.sharedMesh = coneMesh;
          var ccc = cone.AddComponent<ConeColliderController>();
          ccc.enabled = false;
        }
        {
          var line = new GameObject("Line");
          line.transform.parent = axisGo.transform;
          var mf = line.AddComponent<MeshFilter>();
          mf.mesh = tubeMesh;
          var mr = line.AddComponent<MeshRenderer>();
          configureMR(materials[axis], mr);
          var mc = line.AddComponent<CapsuleCollider>();
          mc.isTrigger = true;
          mc.center = new Vector3(0, 0.4f, 0);
          mc.radius = 0.02f;
          mc.height = 0.85f;
          mc.direction = 1;
        }
        var positionAxis = axisGo.AddComponent<PositionAxis>();
        // positionAxis.defaultColor = colors[axis];
      }
      return go;
    }
  }
}