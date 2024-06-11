using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Railloader;
using Track;
using UI.Builder;
using UI.Common;

namespace MapEditor.Dialogs
{
  public sealed class MapEditorDialog : DialogBase
  {

    public MapEditorDialog() : base("Map Editor", 400, 300, Window.Position.UpperLeft)
    {
      _ModMixintoList = EditorContext.ModdingContext.GetMixintos("game-graph").ToList();
      _Names = _ModMixintoList.Select(o => o.Source.ToString()).ToList();
      _Names.Insert(0, "Select ...");
    }

    private readonly List<ModMixinto> _ModMixintoList;
    private readonly List<string> _Names;
    private int _SelectedModMixintoIndex;

    protected override void BuildWindow(UIPanelBuilder builder)
    {
      builder.AddField("Patch", builder.AddDropdown(_Names, _SelectedModMixintoIndex, o =>
      {
        _SelectedModMixintoIndex = o;
        builder.Rebuild();
      })!);

      if (_SelectedModMixintoIndex != 0)
      {
        EditorContext.OpenMixinto(_ModMixintoList[_SelectedModMixintoIndex - 1].Mixinto);

        builder.AddField("Prefix", builder.AddInputField(EditorContext.Prefix ?? "", value => EditorContext.Prefix = value, "Prefix")!);
        builder.HStack(stack =>
        {
          stack.AddButtonCompact("Undo", EditorContext.ChangeManager.Undo);
          stack.AddButtonCompact("Redo", EditorContext.ChangeManager.Redo);
          stack.Spacer();
        });

        builder.AddButton("Save", EditorContext.Save);
        builder.AddButton("Rebuild Track", () =>
        {
          Graph.Shared.RebuildCollections();
          TrackObjectManager.Instance.Rebuild();
        });
      }
      else
      {
        EditorContext.CloseMixinto();
      }

      builder.AddSection("Settings", BuildSettings);
      builder.AddExpandingVerticalSpacer();
    }

    public void BuildSettings(UIPanelBuilder builder)
    {
      builder.AddSection("Map Editor", section =>
      {
        section.AddField("Enabled", section.AddToggle(() => EditorContext.Settings.Enabled, value => EditorContext.Settings.Enabled = value)!);
        section.AddField("Show Helpers", section.AddToggle(() => EditorContext.Settings.ShowHelpers, value => EditorContext.Settings.ShowHelpers = value)!);
      });
    }

    protected override void AfterWindowClosed()
    {
      EditorContext.SaveSettings();
    }

  }
}
