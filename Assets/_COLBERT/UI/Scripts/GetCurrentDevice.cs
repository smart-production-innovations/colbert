using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

//gets the current input device in use (Keyboard and Mouse, Gamepad or Touchscreen)
public class GetCurrentDevice : MonoBehaviour
{
    public enum InputDevice
    {
        None,
        KeyboardAndMouse,
        Touchscreen,
        Gamepad,
    }

    public static InputDevice CurrentInputDevice => currentInputDevice;
    private static InputDevice currentInputDevice = InputDevice.None;

    private void Update()
    {
        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) 
            || (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero))
        {
            currentInputDevice = InputDevice.KeyboardAndMouse;
        }
        else if (Gamepad.current != null
            && (Gamepad.current is XInputController || Gamepad.current is DualShockGamepad)
            && (
            Gamepad.current.allControls.Any(x => x is ButtonControl button && x.IsPressed() && !x.synthetic) 
            || Gamepad.current.leftStick.x.ReadValue() != 0.0f || Gamepad.current.rightStick.x.ReadValue() != 0.0f 
            || Gamepad.current.leftStick.y.ReadValue() != 0.0f || Gamepad.current.rightStick.y.ReadValue() != 0.0f
            ))
        {
            currentInputDevice = InputDevice.Gamepad;
        }
        else if (Touchscreen.current != null
            && Touchscreen.current.press.ReadValue() > 0.0f)
        {
            currentInputDevice = InputDevice.Touchscreen;
        }
    }
}
