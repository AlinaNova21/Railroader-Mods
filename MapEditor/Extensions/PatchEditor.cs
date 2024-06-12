using System;
using System.Linq.Expressions;
using System.Reflection;
using StrangeCustoms.Tracks;
using Track;

namespace MapEditor.Extensions
{
  public static class PatchEditorExtensions
  {
    private static Action<PatchEditor>? _ClearImpl;
    
    public static void UndoAll(this PatchEditor patchEditor)
    {
      while (patchEditor.Undo())
      {
        // noop
      }
    }

    public static void Clear(this PatchEditor patchEditor)
    {
      if (_ClearImpl == null)
      {
        // here I need to mess with private parts of StrangeCustoms mod ...
        // result is method that looks like this:
        // p => {
        //  p.undoRedo.currentIndex = -1;
        //  p.undoRedo.actions.Clear();
        //};
        var undoRedoField = typeof(PatchEditor).GetField("undoRedo", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var undoRedoMagicType = typeof(PatchEditor).Assembly.GetType("StrangeCustoms.Tracks.UndoRedoMagic")!;
        var actionsField = undoRedoMagicType.GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var currentIndexField = undoRedoMagicType.GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var clearMethod = actionsField.FieldType.GetMethod("Clear")!;
        var instance = Expression.Parameter(typeof(PatchEditor), "p");
        var undoRedo = Expression.MakeMemberAccess(instance, undoRedoField);
        var assign = Expression.Assign(Expression.MakeMemberAccess(undoRedo, currentIndexField), Expression.Constant(-1));
        var clear = Expression.Call(Expression.MakeMemberAccess(undoRedo, actionsField), clearMethod);
        var body = Expression.Block(assign, clear);
        var lambda = Expression.Lambda<Action<PatchEditor>>(body, instance);
        _ClearImpl = lambda.Compile();
      }

      _ClearImpl.Invoke(patchEditor);
    }

    public static void AddOrUpdateNode(this PatchEditor patchEditor, TrackNode trackNode)
    {
      patchEditor.AddOrUpdateNode(trackNode.id, trackNode.transform.localPosition, trackNode.transform.localEulerAngles, trackNode.flipSwitchStand);
    }

    public static void AddOrUpdateSegment(this PatchEditor patchEditor, TrackSegment trackSegment)
    {
      patchEditor.AddOrUpdateSegment(trackSegment.id, trackSegment.a.id, trackSegment.b.id, trackSegment.priority, trackSegment.groupId, trackSegment.speedLimit, trackSegment.style, trackSegment.trackClass);
    }

  }
}
