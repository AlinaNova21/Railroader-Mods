using System.Collections;
using System.Reflection;
using Serilog;
using TransformHandles;
using UnityEngine;

namespace MapEditor
{
  internal class Editor : Singleton<Editor>
  {

    private readonly Serilog.ILogger logger = Log.ForContext<Editor>();

    public void Start()
    {
      StartCoroutine(LoadFromMemoryAsync());
    }


    private IEnumerator LoadFromMemoryAsync()
    {
      var rth = Assembly.GetExecutingAssembly().GetManifestResourceStream("MapEditor.Resources.rth.runtime");
      var ms = new System.IO.MemoryStream();
      rth.CopyTo(ms);
      var bytes = ms.ToArray();
      var createRequest = AssetBundle.LoadFromMemoryAsync(bytes);
      yield return createRequest;
      var bundle = createRequest.assetBundle;
      var transformHandlePrefab = bundle.LoadAsset<GameObject>("Assets/RTH.Runtime/Prefabs/NativeTransformHandle.prefab");
      var ghostPrefab = bundle.LoadAsset<GameObject>("Assets/RTH.Runtime/Prefabs/Ghost.prefab");
      TransformHandleManager.Instance.transformHandlePrefab = transformHandlePrefab;
      TransformHandleManager.Instance.ghostPrefab = ghostPrefab;
      TransformHandleManager.Instance.mainCamera = CameraSelector.shared.strategyCamera.CameraContainer.GetComponent<Camera>();
    }

    public void OnEnable()
    {
      logger.Debug("Editor OnEnable()");
    }

    public void OnDisable()
    {
      logger.Debug("Editor OnDisable()");
    }

    public void Update()
    {
      // TransformHandleManager.Instance.mainCamera = Camera.main;
    }

  }
}
