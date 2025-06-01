using Helpers;
using KeyValue.Runtime;
using RollingStock.Controls;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Turntable;
public class RoundhouseComponent : MonoBehaviour
{
  public int Subdivisions { get; set; } = 32;
  public int Stalls { get; set; } = 0;
  public GameObject StartPrefab;
  public GameObject EndPrefab;
  public GameObject StallPrefab;
  private int _lastStalls = 0;
  public void OnEnable()
  {
    this.Build();
  }

  public void Build()
  {
    if (Stalls == _lastStalls) {
      Log.Information("Roundhouse stalls unchanged, skipping");
      return;
    }
    _lastStalls = Stalls;
    Log.Information("Building roundhouse {Stalls}", Stalls);

    var rh = transform.Find("Roundhouse")?.gameObject ?? new GameObject("Roundhouse");
    rh.transform.DestroyAllChildren(); // Reset
    var interval = 360f / Subdivisions;
    rh.transform.parent = transform;
    rh.transform.localPosition = new Vector3(0, -0.48f, 0);
    rh.transform.localEulerAngles = new Vector3(0, 0, 0);
    rh.transform.localScale = new Vector3(1, 1, 1);

    var rhkv = rh.GetComponent<KeyValueObject>() ?? rh.AddComponent<KeyValueObject>();
    var rhgkv = rh.GetComponent<GlobalKeyValueObject>() ?? rh.AddComponent<GlobalKeyValueObject>();

    rhgkv.globalObjectId = name + ".roundhouse";

    if (Stalls < Subdivisions) {
      var start = Instantiate(StartPrefab, rh.transform);
      start.transform.localEulerAngles = interval * Vector3.up;
      PatchDoors(start, $"stall-doors.{0}");

      var end = Instantiate(EndPrefab, rh.transform);
      end.transform.localEulerAngles = interval * Stalls * Vector3.up;
      PatchDoors(end, $"stall-doors.{Stalls - 1}");
    }

    var startPos = Stalls < Subdivisions ? 1 : 0;
    var endPos = Stalls < Subdivisions ? Stalls - 1 : Stalls;
    for (var i = startPos; i < endPos; i++) {
      var angle = (i + 1) * interval;
      var stallInstance = Instantiate(StallPrefab, rh.transform);
      stallInstance.transform.localEulerAngles = angle * Vector3.up;
      PatchDoors(stallInstance, $"stall-doors.{i}");
    }
  }
  private void PatchDoors(GameObject go, string key)
  {
    var kvt = go.GetComponentInChildren<KeyValuePickableToggle>();
    var kva = go.GetComponentInChildren<KeyValueBoolAnimator>();
    if (kvt == null || kva == null) {
      Log.Warning("Missing KeyValuePickableToggle or KeyValueBoolAnimator");
      return;
    }
    kvt.key = key;
    kva.key = key;
  }
}
