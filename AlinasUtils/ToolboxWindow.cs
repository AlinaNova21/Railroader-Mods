using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Game;
using Game.Messages;
using Game.Progression;
using Game.State;
using KeyValue.Runtime;
using Map.Runtime;
using Model.Ops;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasUtils;

internal class ToolboxWindow : WindowBase
{
  public override string WindowIdentifier => "Toolbox";
  public override string Title => "Toolbox";
  public override Vector2Int DefaultSize => new Vector2Int(400, 800);
  public override Window.Position DefaultPosition => Window.Position.UpperRight;
  public override Window.Sizing Sizing => Window.Sizing.Fixed(DefaultSize);
  public bool HideTrees { get; set; } = false;

  public void Show()
  {
    Rebuild();
    var rect = GetComponent<RectTransform>();
    rect.position = new Vector2((float)Screen.width, (float)Screen.height - 40);
    Window.ShowWindow();
  }

  public void Close()
  {
    Window.CloseWindow();
  }

    public override void Populate(UIPanelBuilder builder)
    {
        if (StateManager.IsHost) {
            var setGamemode = (GameMode mode) =>
                StateManager.ApplyLocal(new PropertyChange("_game", "mode", new IntPropertyValue((int)mode)));
            builder.AddSection("Gamemode", gmBuilder => {
                gmBuilder.HStack(b => {
                    b.AddField("Sandbox", b.AddToggle(() => StateManager.Shared.GameMode == GameMode.Sandbox, value => { setGamemode(value ? GameMode.Sandbox : GameMode.Company); }));
                    b.AddField("Company", b.AddToggle(() => StateManager.Shared.GameMode == GameMode.Company, value => { setGamemode(value ? GameMode.Company : GameMode.Sandbox); }));
                });
            });
        }
        builder.AddField("Hide Trees", builder.AddToggle(
            () => HideTrees,
            (value) => {
                HideTrees = value;
                MapManager.Instance!.ForceDisableTrees = value;
                MapManager.Instance!.RebuildAll();
            }
        ));
        if (StateManager.IsHost) {
            builder.AddField("Pause Time", builder.AddToggle(
              () => TimeWeather.TimeMultiplier == 0,
              (value) => {
                  TimeWeather.TimeMultiplier = value ? 0 : 1;
              }
            ));
            builder.AddSection("Sleep");
            builder.HStack(b => {
                var wait = (float hours) => StateManager.ApplyLocal(new WaitTime { Hours = hours });
                builder.AddButtonCompact("1 Hour", () => wait(1));
                builder.AddButtonCompact("3 Hour", () => wait(3));
                builder.AddButtonCompact("6 Hour", () => wait(6));
                builder.AddButtonCompact("Night", () => {
                    var now = TimeWeather.Now.Hours;
                    var tgt = StateManager.Shared.Storage.InterchangeServeHour;
                    var diff = now < tgt ? tgt - now : 24 - now + tgt;
                    wait(diff);
                });
            });
        }

    var areas = OpsController.Shared.Areas;
    var spawnpoints = SpawnPoint.All.Where(sp => {
      var area = OpsController.Shared.ClosestAreaForGamePosition(sp.GamePositionRotation.Item1);
      return area.isActiveAndEnabled;
    });
    var teleports = new List<string> {
      "Teleport..."
    };
    teleports.AddRange(spawnpoints.Select(sp => sp.name));

    var selectedIndex = 0;

    builder.AddField("Teleport", builder.AddDropdown(teleports, selectedIndex, ind => {
      var sp = spawnpoints.ToArray()[ind - 1];
      selectedIndex = 0;
      CameraSelector.shared.JumpToPoint(sp.GamePositionRotation.Item1, sp.GamePositionRotation.Item2, null);
    }));
    builder.AddSection("Progressions", pBuilder => {
      pBuilder.VScrollView(sv => {
        sv.RebuildOnInterval(1);
        var sections = Progression.Shared?.Sections ?? [];
        foreach (var section in sections.Where(s => s.Available || s.Unlocked).OrderBy(s => s.Available ? 0 : 1)) {
          sv.AddField(section.displayName, sv.AddToggle(() => section.Unlocked, (value) => {
            if (value) {
              while (!section.Unlocked) {
                Progression.Shared.Advance(section);
              }
            } else {
              Progression.Shared.Revert(section);
            }
            sv.Rebuild();
          }));
        }
      });
    });
    builder.AddButton("Rebuild Track", () => {
      TrackObjectManager.Instance.Rebuild();
    });
    builder.AddButton("Rebuild Terrain", () => {
        MapManager.Instance.RebuildAll();
    });
    if (StateManager.IsHost) {
        builder.AddButton("Reset Derailments", () => {
            var kv = StateManager.Shared.KeyValueObjectForId("_reputation");
            if (kv == null) return;
            kv["derailments"] = Value.Array([]);
        });
    }

    builder.AddSection("Tooltip", builder => {
      builder.AddLabel(() => {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        var queryTooltipInfo = typeof(ObjectPicker).GetMethod("QueryTooltipInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tooltipInfo = (TooltipInfo)queryTooltipInfo.Invoke(ObjectPicker.Shared, [ray]);
        var title = tooltipInfo.Title;
        if (String.IsNullOrEmpty(title)) title = "Tooltip";
        var text = tooltipInfo.Text;
        if (String.IsNullOrEmpty(text)) text = "No tooltip info";
        return text;
      }, UIPanelBuilder.Frequency.Fast);
    });
    //builder.AddButton("CTC", () =>
    //{
    //  ctcDialog.ShowWindow();
    //});
  }
}
