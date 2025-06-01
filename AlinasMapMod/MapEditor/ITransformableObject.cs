using UnityEngine;

namespace AlinasMapMod.MapEditor;

/// <summary>
/// Interface for objects that can be transformed (moved, rotated, scaled) in the map editor.
/// All of the Can* properties are used to determine if the object can be transformed.
/// The Editor will use these to limit the options available to the user.
/// </summary>
public interface ITransformableObject : IEditableObject
{
  /// <summary>
  /// Check if the object can be moved.
  /// </summary>
  bool CanMove { get; }
  /// <summary>
  /// Check if the object can be rotated.
  /// </summary>
  bool CanRotate { get; }
  /// <summary>
  /// Check if the object can be scaled.
  /// </summary>
  bool CanScale { get; }
  /// <summary>
  /// Get or set the position of the object.
  /// <example>
  /// A basic implementation of this property would be:
  /// <code>
  /// public Vector3 Position {
  ///   get => Transform.localPosition;
  ///   set => Transform.localPosition = value;
  /// }
  /// </code>
  /// Doing it this way allows for more advanced movement options or the ability to run code when the position changes.
  /// </example>
  /// </summary>
  Vector3 Position { get; set; }
  /// <summary>
  /// Get or set the rotation of the object.
  /// <example>
  /// A basic implementation of this property would be:
  /// <code>
  /// public Vector3 Rotation {
  ///   get => Transform.localEulerAngles;
  ///   set => Transform.localEulerAngles = value;
  /// }
  /// </code>
  /// Doing it this way allows for more advanced rotation options or the ability to run code when the rotation changes.
  /// </example>
  /// </summary>
  Vector3 Rotation { get; set; }
  /// <summary>
  /// Get or set the scale of the object.
  /// <example>
  /// A basic implementation of this property would be:
  /// <code>
  /// public Vector3 Scale {
  ///   get => Transform.localScale;
  ///   set => Transform.localScale = value;
  /// }
  /// </code>
  /// Doing it this way allows for more advanced scaling options or the ability to run code when the scale changes.
  /// </example>
  /// </summary>
  Vector3 Scale { get; set; }
  /// <summary>
  /// Get the transform of the object.
  /// <example>
  /// A minimal implementation might be:
  /// <code>
  /// public Transform Transform => transform;
  /// </code>
  /// This assumes that the <see cref="ITransformableObject"/> is a <see cref="MonoBehaviour"/> and is directly on the object being edited. If it's not, you might need to get the transform from a child, parent, or another component.
  /// </example>
  /// </summary>
  Transform Transform { get; }
}
