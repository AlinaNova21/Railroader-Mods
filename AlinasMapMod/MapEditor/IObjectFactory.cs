using System;
using StrangeCustoms.Tracks;

namespace AlinasMapMod.MapEditor;

public interface IObjectFactory
{
  /// <summary>
  /// Returns true if this object type is enabled in the editor.
  /// </summary>
  bool Enabled { get; }
  /// <summary>
  /// Name of the object type. This will be displayed in the editor.
  /// </summary>
  string Name { get; }
  /// <summary>
  /// Build the object in the editor. This will be called when the editor wants to create a new object.
  /// It is fully responsible for creating the object and setting it up.
  /// <example>
  /// For existing splineys, this can be very simple to implement, just implement this interface on the same class as <see cref="StrangeCustoms.ISplineyBuilder"/>
  /// and do something like this:
  /// <code>
  /// public IEditableObject CreateObject(PatchEditor editor, string id)
  /// {
  ///   var data = new SerializedMapLabel
  ///   {
  ///     Text = "New Map Label"
  ///   };
  ///   var obj = BuildSpliney(id, null, JObject.FromObject(data));
  ///   return obj.GetComponent&lt;EditableMapLabel&gt;();
  /// }
  /// </code>
  /// </example>
  /// </summary>
  /// <param name="editor"></param>
  /// <param name="id"></param>
  /// <returns></returns>
  IEditableObject CreateObject(PatchEditor editor, string id);
  /// <summary>
  /// Type of the object. This will be used to determine what type of object this is.
  /// <example>
  /// For example, if this is a loader, it will be <code>typeof(<see cref="Loaders.LoaderInstance"/>)</code>
  /// </example>
  /// </summary>
  Type ObjectType { get; }
}
