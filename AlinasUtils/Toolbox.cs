using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Character;
using Game;
using Game.Messages;
using Game.Progression;
using Game.State;
using HarmonyLib;
using Model.OpsNew;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace AlinasUtils;

class Toolbox
{
  private Window window;
  public Toolbox()
  {

  }

  public void Init()
  {
    if (window != null)
    {
      window.CloseWindow();
      UnityEngine.Object.Destroy(window.gameObject);
    }
    var helper = AlinasUtilsPlugin.Shared.UIHelper;
    window = helper.CreateWindow(400, 600, Window.Position.UpperRight);
    var rectTransformProp = AccessTools.Property(typeof(Window), "RectTransform");
    var rectTransform = rectTransformProp.GetValue(window) as RectTransform;
    rectTransform.position -= new Vector3(0, 40, 0);
    window.Title = "Toolbox";
    helper.PopulateWindow(window, BuildWindow);

    var tr = UnityEngine.Object.FindObjectOfType<TopRightArea>();
    if (tr != null)
    {
      var buttons = tr.transform.Find("Buttons");
      var go = new GameObject("AlinasUtilsButton");
      go.transform.parent = buttons;
      var button = go.AddComponent<Button>();
      button.onClick.AddListener(() =>
      {
        window.ShowWindow();
      });
      var rawIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("AlinasUtils.Resources.toolbox-icon.png");
      var ms = new System.IO.MemoryStream();
      rawIcon.CopyTo(ms);
      var iconData = ms.ToArray();
      var icon = go.AddComponent<Image>();
      var tex = new Texture2D(36, 36, TextureFormat.ARGB32, false);
      ImageConversion.LoadImage(tex, iconData);
      icon.sprite = Sprite.Create(tex, new Rect(0, 0, 36, 36), new Vector2(0.5f, 0.5f));
      icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
      icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
    }
  }

  private void BuildWindow(UIPanelBuilder builder)
  {
    if (StateManager.IsHost)
    {
      var setGamemode = (GameMode mode) =>
        StateManager.ApplyLocal(new PropertyChange("_game", "mode", new IntPropertyValue((int)mode)));
      builder.AddSection("Gamemode", gmBuilder =>
      {
        gmBuilder.HStack(b =>
        {
          b.AddField("Sandbox", b.AddToggle(() => StateManager.Shared.GameMode == GameMode.Sandbox, value => { setGamemode(value ? GameMode.Sandbox : GameMode.Company); }));
          b.AddField("Company", b.AddToggle(() => StateManager.Shared.GameMode == GameMode.Company, value => { setGamemode(value ? GameMode.Company : GameMode.Sandbox); }));
        });
      });
    }
    builder.AddSection("Sleep");
    builder.HStack(b =>
    {
      var wait = (float hours) => StateManager.ApplyLocal(new WaitTime { Hours = hours });
      builder.AddButtonCompact("1 Hour", () => wait(1));
      builder.AddButtonCompact("Night", () =>
      {
        var now = TimeWeather.Now.Hours;
        var tgt = StateManager.Shared.Storage.InterchangeServeHour;
        var diff = now < tgt ? tgt - now : 24 - now + tgt;
        wait(diff);
      });
    });
    var areas = OpsController.Shared.Areas;
    var spawnpoints = SpawnPoint.All.Where(sp =>
    {
      var area = OpsController.Shared.ClosestAreaForGamePosition(sp.GamePositionRotation.Item1);
      return area.isActiveAndEnabled;
    });
    var teleports = new List<string> {
      "Teleport..."
    };
    teleports.AddRange(spawnpoints.Select(sp => sp.name));

    var selectedIndex = 0;

    builder.AddField("Teleport", builder.AddDropdown(teleports, selectedIndex, ind =>
    {
      var sp = spawnpoints.ToArray()[ind - 1];
      selectedIndex = 0;
      CameraSelector.shared.JumpToPoint(sp.GamePositionRotation.Item1, sp.GamePositionRotation.Item2, null);
    }));
    builder.AddSection("Progressions", pBuilder =>
    {
      pBuilder.VScrollView(sv =>
      {
        sv.RebuildOnInterval(1);
        var sections = Progression.Shared?.Sections ?? [];
        foreach (var section in sections.Where(s => s.Available || s.Unlocked).OrderBy(s => s.Available ? 0 : 1))
        {
          sv.AddField(section.displayName, sv.AddToggle(() => section.Unlocked, (value) =>
          {
            if (value)
            {
              while (!section.Unlocked)
              {
                Progression.Shared.Advance(section);
              }
            }
            else
            {
              Progression.Shared.Revert(section);
            }
            sv.Rebuild();
          }));
        }
      });
    });
    builder.AddButton("Rebuild Track", () =>
    {
      TrackObjectManager.Instance.Rebuild();
    });
  }

  private IEnumerator WaitForSections(UIPanelBuilder builder)
  {
    while (Progression.Shared == null)
    {
      yield return new WaitForSeconds(1);
    }
    builder.Rebuild();
    yield break;
  }
}
