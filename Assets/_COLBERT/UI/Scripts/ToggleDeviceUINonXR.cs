using UnityEngine;

//toggle active ui depending on input device
public class ToggleDeviceUINonXR : MonoBehaviour
{
    [SerializeField] private GameObject keyboardMouseUI;
    [SerializeField] private GameObject gamepadUI;
    [SerializeField] private GameObject touchscreenUI;

    private GetCurrentDevice.InputDevice prevInputDevice = GetCurrentDevice.InputDevice.None;

    //For Android Build
    [SerializeField] private GameObject spawnXRButton;

    void Start()
    {
        SetNoneUIActive();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        spawnXRButton.SetActive(true);
        keyboardMouseUI.SetActive(true);
        touchscreenUI.SetActive(false);
#else 
        spawnXRButton.SetActive(false);
        keyboardMouseUI.SetActive(false);
        touchscreenUI.SetActive(true);        
#endif
    }
    private void Update()
    {
        if(GetCurrentDevice.CurrentInputDevice != prevInputDevice)
        {
            switch (GetCurrentDevice.CurrentInputDevice)
            {
                case GetCurrentDevice.InputDevice.None:
                    SetNoneUIActive();
                    prevInputDevice = GetCurrentDevice.CurrentInputDevice;
                    break;

                case GetCurrentDevice.InputDevice.KeyboardAndMouse:
                    SetKeyboardMouseUIActive();
                    prevInputDevice = GetCurrentDevice.CurrentInputDevice;
                    break;

                case GetCurrentDevice.InputDevice.Touchscreen:
                    SetTouchUIActive();
                    prevInputDevice = GetCurrentDevice.CurrentInputDevice;
                    break;

                case GetCurrentDevice.InputDevice.Gamepad:
                    SetGamepadUIActive();
                    prevInputDevice = GetCurrentDevice.CurrentInputDevice;
                    break;
            }
        }
    }

    private void SetKeyboardMouseUIActive()
    {
        touchscreenUI.SetActive(false);
        keyboardMouseUI.SetActive(true);
        gamepadUI.SetActive(false);
    }

    private void SetGamepadUIActive()
    {
        touchscreenUI.SetActive(false);
        keyboardMouseUI.SetActive(false);
        gamepadUI.SetActive(true);
    }

    private void SetTouchUIActive()
    {
        touchscreenUI.SetActive(true);
        keyboardMouseUI.SetActive(false);
        gamepadUI.SetActive(false);
    }

    private void SetNoneUIActive()
    {
        touchscreenUI.SetActive(false);
        keyboardMouseUI.SetActive(false);
        gamepadUI.SetActive(false);
    }
}
