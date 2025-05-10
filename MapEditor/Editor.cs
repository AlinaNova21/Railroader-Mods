using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using Serilog;
using TransformHandles;
using UnityEngine;

namespace MapEditor
{
  [UsedImplicitly]
  internal class Editor : Singleton<Editor>
  {

    private readonly Serilog.ILogger logger = Log.ForContext<Editor>();

    [UsedImplicitly]
    public void Start()
    {
      //StartCoroutine(LoadFromMemoryAsync());
    }

    private IEnumerator LoadFromMemoryAsync()
    {
      byte[] bytes;
      using (var ms = new System.IO.MemoryStream())
      {
        using var rth = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapEditor.Resources.rth.runtime")!;
        rth.CopyTo(ms);
        bytes = ms.ToArray();
      }

      var createRequest = AssetBundle.LoadFromMemoryAsync(bytes)!;
      yield return createRequest;
      var bundle = createRequest.assetBundle!;
      var transformHandlePrefab = bundle.LoadAsset<GameObject>("Assets/RTH.Runtime/Prefabs/NativeTransformHandle.prefab")!;
      var ghostPrefab = bundle.LoadAsset<GameObject>("Assets/RTH.Runtime/Prefabs/Ghost.prefab")!;
      TransformHandleManager.Instance.transformHandlePrefab = transformHandlePrefab;
      TransformHandleManager.Instance.ghostPrefab = ghostPrefab;
      TransformHandleManager.Instance.mainCamera = CameraSelector.shared.strategyCamera.CameraContainer.GetComponent<Camera>()!;
    }

    [UsedImplicitly]
    public void OnEnable()
    {
      logger.Debug("Editor OnEnable()");
    }

    [UsedImplicitly]
    public void OnDisable()
    {
      logger.Debug("Editor OnDisable()");
    }

    [UsedImplicitly]
    public void Update()
    {
      // TransformHandleManager.Instance.mainCamera = Camera.main;
    }

  }
}
