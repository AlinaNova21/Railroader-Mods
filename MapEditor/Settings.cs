using UnityEngine;

namespace MapEditor
{
  public sealed class Settings
  {
    public KeyCode MoveForward { get; set; } = KeyCode.Keypad8;
    public KeyCode MoveBackward { get; set; } = KeyCode.Keypad5;
    public KeyCode MoveLeft { get; set; } = KeyCode.Keypad4;
    public KeyCode MoveRight { get; set; } = KeyCode.Keypad6;
    public KeyCode MoveUp { get; set; } = KeyCode.Keypad9;
    public KeyCode MoveDown { get; set; } = KeyCode.Keypad7;
    public KeyCode ToggleMode { get; set; } = KeyCode.KeypadEnter;
    public KeyCode IncrementScaling { get; set; } = KeyCode.KeypadPlus;
    public KeyCode DecrementScaling { get; set; } = KeyCode.KeypadMinus;
    public KeyCode MultiplyScalingDelta { get; set; } = KeyCode.KeypadMultiply;
    public KeyCode DivideScalingDelta { get; set; } = KeyCode.KeypadDivide;
    public KeyCode ResetScaling { get; set; } = KeyCode.Keypad0;

  }
}
