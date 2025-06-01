using System.Linq;
using Track;
using Track.Signals;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasUtils;

internal class CTCDialog
{
  private bool _Populated;
  private readonly Window _Window;

  public CTCDialog()
  {
    _Window = AlinasUtilsPlugin.Shared.UIHelper.CreateWindow("ctc-dialog", 400, 600, Window.Position.Center);
    _Window.Title = "CTC";

    _Window.OnShownDidChange += shown => {
      if (!shown) {
        AfterWindowClosed();
      }
    };
  }

  protected void BuildWindow(UIPanelBuilder builder)
  {
    builder.VScrollView(builder => {
      var ctc = GameObject.FindObjectOfType<CTCSwitchMonitor>();
      if (ctc == null) {
        builder.AddLabel("CTC not found");
        return;
      }
      builder.AddLabel("CTC Stuff");
      foreach (var transform in ctc.transform) {
        if (transform is Transform t) {
          var go = t.gameObject;
          if (!go.activeInHierarchy) continue;
          if (!go.GetComponent<CTCMapFeatureTarget>()) continue;
          builder.AddSection(go.name, b => BuildModule(b, go));
        }
      }
    });
  }

  private void BuildModule(UIPanelBuilder builder, GameObject module)
  {
    foreach (var transform1 in module.transform) {
      if (transform1 is Transform t1) {
        var part = t1.gameObject;
        if (!part.activeInHierarchy)
          continue;
        builder.AddLabel($"Section {part.name}");
        var interlocking = part.GetComponent<CTCInterlocking>();
        if (interlocking != null) {
          builder.AddLabel("Interlocking Found");
        }
        var intermediate = part.GetComponent<CTCIntermediate>();
        if (intermediate != null) {
          builder.AddLabel("Intermediate Found");
        }
        foreach (var transform in part.transform) {
          if (transform is Transform t) {
            var go = t.gameObject;
            var block = go.GetComponent<CTCBlock>();
            if (block != null) {
              builder.AddLabel($"Block: {block.name}");
              builder.AddLabel($"  Thrown Switch Sets Occupied {block.thrownSwitchesSetOccupied}");
              builder.AddField("  Occupied", () => block.IsOccupied.ToString(), UIPanelBuilder.Frequency.Periodic);
              var spans = block.GetComponentsInChildren<TrackSpan>();
              var spanIds = spans.ToList().Select(s => s.id).ToArray();
              var rt = builder.AddLocationFieldFallback($"  Spans ({spanIds})", () => { });
              var hover = rt.GetComponentInChildren<LocationIndicatorHoverArea>();
              hover.spanIds.AddRange(spanIds);
              var center = spans[0].GetCenterPoint();
              hover.descriptors.Add(new LocationIndicatorController.Descriptor(center, block.name, "CTC Block"));

            }
            var signal = go.GetComponent<CTCAutoSignal>();
            if (signal != null) {
              builder.AddLabel($"Signal: {signal.name}");
            }
          }
        }
      }
    }
  }

  public void ShowWindow()
  {
    //if (!_Populated)
    //{
    AlinasUtilsPlugin.Shared.UIHelper.PopulateWindow(_Window, BuildWindow);
    //_Populated = true;
    //}

    BeforeWindowShown();

    if (!_Window.IsShown) {
      _Window.ShowWindow();
    }
  }

  public void CloseWindow()
  {
    _Window.CloseWindow();
  }

  protected virtual void BeforeWindowShown() { }

  protected virtual void AfterWindowClosed() { }
}
