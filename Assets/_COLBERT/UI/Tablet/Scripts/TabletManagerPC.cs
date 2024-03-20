using UnityEngine;
using UnityEngine.InputSystem;

//handles visibility of non-xr tablet and cursor and block other input when tablet is open
public class TabletManagerPC : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private InputActionProperty toggleTablet;
    [SerializeField] private InputActionAsset inputActions;

    private void Awake()
    {
        canvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        toggleTablet.action.performed += ToggleTablet;
    }

    private void OnDisable()
    {
        toggleTablet.action.performed -= ToggleTablet;
    }

    private void Update()
    {
        //For when closed due to joining Lobby
        if (!canvas.gameObject.activeSelf && !inputActions.FindActionMap("NonXRPlayer").enabled)
            EnableAllOtherControls();
    }

    private void DisableAllOtherControls()
    {
        ToggleCursor.SetOverride(true);
        PlayerController.Lock();
        inputActions.FindActionMap("NonXRPlayer").Disable();
    }

    private void EnableAllOtherControls()
    {
        ToggleCursor.ReleaseOverride();
        PlayerController.Unlock();
        inputActions.FindActionMap("NonXRPlayer").Enable();
    }

    private void ToggleTablet(InputAction.CallbackContext obj)
    {
        if (!canvas.isActiveAndEnabled)
        {
            canvas.gameObject.SetActive(true);
            DisableAllOtherControls();
        }
        else
        {
            canvas.gameObject.SetActive(false);
            EnableAllOtherControls();
        }
    }

    public void CloseTablet()
    {
        if (!canvas.gameObject.activeSelf)
            return;

        canvas.gameObject.SetActive(false);
        EnableAllOtherControls();
    }

}
