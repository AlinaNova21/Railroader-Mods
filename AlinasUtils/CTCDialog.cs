using System.Linq;
using Track;
using Track.Signals;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasUtils;

internal class CTCDialog : WindowBase
{
  private readonly Window _Window;

  public override string WindowIdentifier => "ctc-dialog";

  public override string Title => "CTC";

  public override Vector2Int DefaultSize => new(400, 600);

  public override Window.Position DefaultPosition => Window.Position.Center;

  public override Window.Sizing Sizing => Window.Sizing.Fixed(DefaultSize);


  public void Show()
  {
    Rebuild();
    Window.ShowWindow();
  }

  public void Close()
  {
    Window.CloseWindow();
  }

  public override void Populate(UIPanelBuilder builder)
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
}
