using UnityEngine;
using UnityEngine.InputSystem;

//Script for Closing the Application with and Esc Menu
public class CloseApplication : MonoBehaviour
{
    [SerializeField] private Canvas menuUI;

    [SerializeField] private InputActionProperty toggleMenu;

    private void OnEnable()
    {
        toggleMenu.action.performed += ToggleMenu;
    }
    private void OnDisable()
    {
        toggleMenu.action.performed -= ToggleMenu;
    }

    private void ToggleMenu(InputAction.CallbackContext obj)
    {
        if (!menuUI.isActiveAndEnabled)
        {
            menuUI.gameObject.SetActive(true);
            ToggleCursor.SetOverride(true);
            PlayerController.Lock();
        }
        else
        {
            menuUI.gameObject.SetActive(false);
            ToggleCursor.ReleaseOverride();
            PlayerController.Unlock();
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
