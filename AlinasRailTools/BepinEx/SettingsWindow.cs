using System;
using System.Linq;
using BepInEx.Configuration;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasUtils.BepinEx;
//internal class SettingsWindow : WindowBase
//{
//  public override string WindowIdentifier => "settings";

//  public override string Title => "Settings";

//  public override Vector2Int DefaultSize => new(1000,800);

//  public override Window.Position DefaultPosition => Window.Position.Center;

//  public override Window.Sizing Sizing => Window.Sizing.Fixed(DefaultSize);

//  private ConfigFile ConfigFile { get; set; }

//  public void Show(ConfigFile configFile)
//  {
//    if (ConfigFile != null) {
//      ConfigFile.ConfigReloaded -= ConfigReloaded;
//    }
//    ConfigFile = configFile;
//    ConfigFile.ConfigReloaded += ConfigReloaded;
//    Rebuild();
//    Window.ShowWindow();
//  }

//  private void ConfigReloaded(object sender, EventArgs e) => Rebuild();

//  public override void Populate(UIPanelBuilder builder)
//  {
//    var groups = ConfigFile
//      .Keys
//      .GroupBy(e => e.Section)
//      .ToDictionary(g => g.Key, g => g.ToList());

//    foreach (var group in groups) {
//      builder.AddSection(group.Key, b => {
//        foreach (var entry in group.Value) {
//          var ce = ConfigFile[entry];
//          if (ce != null) {
//            switch (ce) {
//              case ConfigEntry<bool> boolE:
//                b.AddField(ce.Definition.Key, b.AddToggle(() => boolE.Value, value => boolE.Value = value));
//                b.AddLabel(ce.Description.Description);
//                break;
//              case ConfigEntry<int> intE:
//                b.AddField(ce.Definition.Key, b.AddInputField(intE.Value.ToString(), value => intE.Value = int.Parse(value)));
//                b.AddLabel(ce.Description.Description);
//                break;
//              case ConfigEntry<float> floatE:
//                b.AddField(ce.Definition.Key, b.AddInputField(floatE.Value.ToString(), value => floatE.Value = float.Parse(value)));
//                b.AddLabel(ce.Description.Description);
//                break;
//              case ConfigEntry<string> stringE:
//                b.AddField(ce.Definition.Key, b.AddInputField(stringE.Value, value => stringE.Value = value));
//                b.AddLabel(ce.Description.Description);
//                break;
//            }
//          }
//        }
//      });
//    }
//  }

//  public void ConfigEntryField<T>(UIPanelBuilder builder, ConfigEntry<T> ce)
//  {

//  }
//}
