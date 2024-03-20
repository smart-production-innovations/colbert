using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

//additional script for adding haptics when hovering with xr controller over UI element
public class PokerHaptics : MonoBehaviour
{
    [SerializeField] private ActionBasedController controller;

    [Range(0f, 1f)]
    public float hoverHapticIntensity;
    public float hoverHapticDuration;

    [Range(0f, 1f)]
    public float selectHapticIntensity;
    public float selectHapticDuration;
    public void OnUIHoverEnter(UIHoverEventArgs args)
    {
        controller.SendHapticImpulse(hoverHapticIntensity, hoverHapticDuration);
        if (args.uiObject.GetComponentInParent<Toggle>() != null) args.uiObject.GetComponentInParent<Toggle>().onValueChanged.AddListener(OnToggleClick);
        if (args.uiObject.TryGetComponent(out Button button))  button.onClick.AddListener(OnButtonClick);
    }

    public void OnUIHoverExit(UIHoverEventArgs args)
    {
        if (args.uiObject.GetComponentInParent<Toggle>() != null) args.uiObject.GetComponentInParent<Toggle>().onValueChanged.RemoveListener(OnToggleClick);
        if (args.uiObject.TryGetComponent(out Button button)) button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnToggleClick(bool isOn)
    {
        controller.SendHapticImpulse(selectHapticIntensity, selectHapticDuration);
    }
    public void OnButtonClick()
    {
        controller.SendHapticImpulse(selectHapticIntensity, selectHapticDuration);
    }
}
