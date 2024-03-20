using UnityEngine;
using UnityEngine.InputSystem;

//touchscreen also provides mouse inputs -> this script disables the mouse, if touch input is detected
public class DisableMouseOnTouch : MonoBehaviour
{
    [SerializeField]
    private InputActionProperty touchAction;
    [SerializeField]
    private float touchTimeout = 0.1f;

    private float latestTouchTime = 0.0f;
    private bool touchContact = false;


    private void OnEnable()
    {
        touchAction.action.Enable();
        touchAction.action.performed += OnTouch;
    }

    private void OnDisable()
    {
        touchAction.action.Disable();
        touchAction.action.performed -= OnTouch;
        EnableMouse();
    }

    private void Update()
    {
        if (!touchContact && Time.time > latestTouchTime + touchTimeout)
        {
            EnableMouse();
        }
    }

    private void OnTouch(InputAction.CallbackContext obj)
    {
        touchContact = obj.ReadValue<float>() > 0;
        latestTouchTime = Time.deltaTime;
        DisableMouse();
    }

    private void EnableMouse()
    {
        if (Mouse.current != null)
        {
            InputSystem.EnableDevice(Mouse.current);
        }
    }

    private void DisableMouse()
    {
        if (Mouse.current != null)
        {
            Cursor.visible = false;
            InputSystem.DisableDevice(Mouse.current);
        }
    }
}
