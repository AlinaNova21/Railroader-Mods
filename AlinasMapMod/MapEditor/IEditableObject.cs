using System.Collections.Generic;
using StrangeCustoms.Tracks;
using UI.Builder;

namespace AlinasMapMod.MapEditor;

/// <summary>
/// Interface for editable objects in the map editor.
/// The Can* properties will be used by the editor for UI and functionality.
/// </summary>
public interface IEditableObject
{
  /// <summary>
  /// Id of the object.
  /// </summary>
  string Id { get; set; }

  /// <summary>
  /// Type of the object, this will be displayed in the UI, Ex: "Loader"
  /// </summary>
  string DisplayType { get; }

  /// <summary>
  /// Allow the object to be edited.
  /// </summary>
  bool CanEdit { get; }
  /// <summary>
  /// Allow the object to be created. Note that this also requires a seperate class implenting <see cref="IObjectFactory"/>
  /// </summary>
  bool CanCreate { get; }
  /// <summary>
  /// Allow the object to be destroyed.
  /// </summary>
  bool CanDestroy { get; }

  /// <summary>
  /// List of properties that can be edited for this object.
  /// </summary>
  List<string> Properties { get; }

  /// <summary>
  /// Set a property for this object. This will be called when the editor wants to set a property.
  /// </summary>
  /// <param name="property"></param>
  /// <param name="value"></param>
  void SetProperty(string property, object value);
  /// <summary>
  /// Get a property for this object. This will be called when the editor wants to get a property.
  /// </summary>
  /// <param name="property"></param>
  /// <returns></returns>
  object GetProperty(string property);
  /// <summary>
  /// Build the UI for this object. This will be called when the editor wants to build the UI for this object.
  /// Use the <see cref="UIPanelBuilder"/> to build the UI.
  /// Use the <see cref="IEditorUIHelper"/> to get or set the current value of a property. This ensures history state is tracked.
  /// </summary>
  /// <param name="builder"></param>
  /// <param name="helper"></param>
  void BuildUI(UIPanelBuilder builder, IEditorUIHelper helper);
  /// <summary>
  /// Save the object to the editor. This will be called when the editor wants to save the object.
  /// This method is responsible for calling the relevent PatchEditor methods to save the object. Ex: <see cref="PatchEditor.AddOrUpdateSpliney(string, System.Func{Newtonsoft.Json.Linq.JObject?, Newtonsoft.Json.Linq.JObject})"/>
  /// It should ensure the handler is set, and all relevent properties if the object is a spliney
  /// </summary>
  /// <param name="editor"></param>
  void Save(PatchEditor editor);
  /// <summary>
  /// Load the object from the editor. This will be called when the editor wants to load the object.
  /// This method is responsible for calling the relevent PatchEditor methods to load the object. Ex: <see cref="PatchEditor.GetSpliney(string)"/>
  /// It should then populate the properties of the object with the data.
  /// </summary>
  /// <param name="editor"></param>
  void Load(PatchEditor editor);
  /// <summary>
  /// Destroy the object. This will be called when the editor wants to destroy the object.
  /// It should remove the object from the world and update any relevant game state.
  /// </summary>
  /// <param name="editor"></param>
  void Destroy(PatchEditor editor);
}
