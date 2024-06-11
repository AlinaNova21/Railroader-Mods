using JetBrains.Annotations;
using UnityEngine;

namespace MapEditor.Managers
{
  public sealed class KeyboardManager : MonoBehaviour
  {

    private static GameObject? _gameObject;

    public static void Activate()
    {
      if (_gameObject == null)
      {
        _gameObject = new GameObject("KeyboardManager");
        _gameObject.AddComponent<KeyboardManager>();
      }

      _gameObject.SetActive(true);
    }

    public static void Deactivate()
    {
      _gameObject!.SetActive(false);
    }

    public static bool Rotate { get; private set; }

    [UsedImplicitly]
    public void Update()
    {
      if (EditorContext.SelectedNode != null)
      {
        UpdateNode();
      }

      if (EditorContext.SelectedSegment != null)
      {
        UpdateSegment();
      }
    }

    private static void UpdateNode()
    {
      if (Input.GetKeyDown(KeyCode.KeypadEnter))
      {
        Rotate = !Rotate;
      }

      if (Input.GetKeyDown(KeyCode.Keypad4))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.down);
        }
        else
        {
          NodeManager.Move(Direction.left);
        }
      }

      if (Input.GetKeyDown(KeyCode.Keypad5))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.left);
        }
        else
        {
          NodeManager.Move(Direction.backward);
        }
      }

      if (Input.GetKeyDown(KeyCode.Keypad6))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.up);
        }
        else
        {
          NodeManager.Move(Direction.right);
        }
      }

      if (Input.GetKeyDown(KeyCode.Keypad7))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.back);
        }
        else
        {
          NodeManager.Move(Direction.down);
        }
      }

      if (Input.GetKeyDown(KeyCode.Keypad8))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.right);
        }
        else
        {
          NodeManager.Move(Direction.forward);
        }
      }

      if (Input.GetKeyDown(KeyCode.Keypad9))
      {
        if (Rotate)
        {
          NodeManager.Rotate(Vector3.forward);
        }
        else
        {
          NodeManager.Move(Direction.up);
        }
      }

      if (Input.GetKeyDown(KeyCode.KeypadPlus))
      {
        NodeManager.IncrementScaling();
      }

      if (Input.GetKeyDown(KeyCode.KeypadMinus))
      {
        NodeManager.DecrementScaling();
      }

      if (Input.GetKeyDown(KeyCode.KeypadMultiply))
      {
        NodeManager.MultiplyScalingDelta();
      }

      if (Input.GetKeyDown(KeyCode.KeypadDivide))
      {
        NodeManager.DivideScalingDelta();
      }

      if (Input.GetKeyDown(KeyCode.Keypad0))
      {
        NodeManager.ResetScaling();
      }
    }

    private static void UpdateSegment()
    {
      var segment = EditorContext.SelectedSegment!;

      if (Input.GetKeyDown(KeyCode.Keypad7))
      {
        NodeManager.Move(Direction.down, segment.a);
        NodeManager.Move(Direction.down, segment.b);
      }

      if (Input.GetKeyDown(KeyCode.Keypad9))
      {
        NodeManager.Move(Direction.up, segment.a);
        NodeManager.Move(Direction.up, segment.b);
      }
    }

  }
}
