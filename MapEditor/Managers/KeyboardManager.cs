using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using UnityEngine;

namespace MapEditor.Managers
{
  public sealed class KeyboardManager : MonoBehaviour
  {

    private static readonly Serilog.ILogger _Logger = Log.ForContext(typeof(KeyboardManager));

    private static GameObject? _gameObject;

    public static void Activate()
    {
      if (_gameObject == null) {
        _gameObject = new GameObject("KeyboardManager");
        _gameObject.AddComponent<KeyboardManager>();
      }

      _gameObject.SetActive(true);
      BuildActionDictionaries();
    }

    public static void Deactivate()
    {
      _gameObject!.SetActive(false);
    }

    // actions, that can be executed when node editor is open
    private static readonly Dictionary<KeyCode, Action> _NodeActions = new Dictionary<KeyCode, Action>();

    // actions, that can be executed when segment editor is open
    private static readonly Dictionary<KeyCode, Action> _SegmentActions = new Dictionary<KeyCode, Action>();

    // actions, that can be executed when node or segment editor is open
    private static readonly Dictionary<KeyCode, Action> _CommonActions = new Dictionary<KeyCode, Action>();

    private static void BuildActionDictionaries()
    {
      _NodeActions.Clear();
      _SegmentActions.Clear();
      _CommonActions.Clear();

      if (EditorContext.Settings.MoveForward != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveForward, NodeForward);
      }

      if (EditorContext.Settings.MoveBackward != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveBackward, NodeBackward);
      }

      if (EditorContext.Settings.MoveLeft != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveLeft, NodeLeft);
      }

      if (EditorContext.Settings.MoveRight != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveRight, NodeRight);
      }

      if (EditorContext.Settings.MoveUp != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveUp, NodeUp);
        _SegmentActions.Add(EditorContext.Settings.MoveUp, SegmentUp);
      }

      if (EditorContext.Settings.MoveDown != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.MoveDown, NodeDown);
        _SegmentActions.Add(EditorContext.Settings.MoveDown, SegmentDown);
      }

      if (EditorContext.Settings.ToggleMode != KeyCode.None) {
        _NodeActions.Add(EditorContext.Settings.ToggleMode, NodeToggle);
      }
    }

    public static bool Rotate { get; private set; }


    [UsedImplicitly]
    public void Update()
    {
      if (EditorContext.SelectedNode != null) {
        ProcessActions(_CommonActions);
        ProcessActions(_NodeActions);
      }

      if (EditorContext.SelectedSegment != null) {
        ProcessActions(_CommonActions);
        ProcessActions(_SegmentActions);
      }
    }

    private void ProcessActions(Dictionary<KeyCode, Action> actions)
    {
      foreach (var pair in actions) {
        if (Input.GetKeyDown(pair.Key)) {
          _Logger.Information("KeyDown: " + pair.Key);
          pair.Value!();
        }
      }
    }

    private static void NodeToggle()
    {
      Rotate = !Rotate;
    }

    private static void NodeUp()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.forward);
      } else {
        NodeManager.Move(Direction.up);
      }
    }

    private static void NodeDown()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.back);
      } else {
        NodeManager.Move(Direction.down);
      }
    }

    private static void NodeForward()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.right);
      } else {
        NodeManager.Move(Direction.forward);
      }
    }

    private static void NodeBackward()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.left);
      } else {
        NodeManager.Move(Direction.backward);
      }
    }

    private static void NodeLeft()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.down);
      } else {
        NodeManager.Move(Direction.left);
      }
    }

    private static void NodeRight()
    {
      if (Rotate) {
        NodeManager.Rotate(Vector3.up);
      } else {
        NodeManager.Move(Direction.right);
      }
    }

    private static void SegmentUp()
    {
      SegmentManager.Move(Direction.up);
    }

    private static void SegmentDown()
    {
      SegmentManager.Move(Direction.down);
    }

  }
}
