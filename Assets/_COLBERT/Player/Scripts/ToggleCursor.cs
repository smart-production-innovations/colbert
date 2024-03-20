using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// show/hide mouse cursor on button press
// if hidden, the mouse cursor is locked to the center of the screen
public class ToggleCursor : MonoBehaviour
{
    [SerializeField]
    private InputActionProperty showMouseAction;

    private bool toggleValue = false;

    private bool overrideActive = false;
    private bool overrideValue = false;

    private static ToggleCursor instance = null;
    public static ToggleCursor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ToggleCursor>();
            }
            return instance;
        }
    }

    public static void SetOverride(bool active)
    {
        ToggleCursor tc = Instance;
        tc.overrideActive = true;
        tc.overrideValue = active;
        tc.SetCursor(tc.overrideValue);
    }

    public static void ReleaseOverride()
    {
        ToggleCursor tc = Instance;
        tc.overrideActive = false;
        tc.SetCursor(tc.toggleValue);
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnEnable()
    {
        DisableCursor();
        showMouseAction.action.Enable();
    }

    private void OnDisable()
    {
        EnableCursor();
        showMouseAction.action.Disable();
    }

    private void Update()
    {
        toggleValue = showMouseAction.action.ReadValue<float>() > 0.0f;
        if (!overrideActive)
        {
            SetCursor(toggleValue);
        }
    }

    private void EnableCursor()
    {
        SetCursor(true);
    }

    private void DisableCursor()
    {
        SetCursor(false);
    }

    private void SetCursor(bool enabled)
    {
        bool inWindow = true;
        if (Mouse.current != null)
        {
            Vector3 mousepos = Mouse.current.position.ReadValue();
            int margin = 50;
            inWindow = mousepos.x >= 0 + margin && mousepos.y >= 0 + margin && mousepos.x < Screen.width - margin && mousepos.y < Screen.height - margin;
        }

        if (enabled || !Application.isFocused || (UnityEngine.Cursor.lockState == CursorLockMode.None && (!inWindow || (Mouse.current != null && !Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.rightButton.wasPressedThisFrame))))
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None; //Confined ?
            SetCursorVisibility();
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            SetCursorVisibility();
        }
    }

    private void SetCursorVisibility()
    {
        UnityEngine.Cursor.visible = !Application.isFocused || UnityEngine.Cursor.lockState == CursorLockMode.None;
    }

}
