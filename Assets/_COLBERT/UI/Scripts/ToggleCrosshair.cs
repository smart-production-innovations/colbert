using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//toggle crosshair and laser indicator on crosshair
public class ToggleCrosshair : MonoBehaviour
{
    [SerializeField]
    private Image crosshair;

    [SerializeField]
    private Image laserIndicator;

    [SerializeField]
    private GameObject laserIsActive;

    private PlayerManager playerManager = null;

    private void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
    }

    private void Update()
    {
        if (!playerManager.PlayerNonXR.IsActive)
        {
            laserIndicator.enabled = false;
            return;
        }

        if (GetCurrentDevice.CurrentInputDevice == GetCurrentDevice.InputDevice.KeyboardAndMouse)
        {
            if (Cursor.visible)
            {
                crosshair.enabled = false;
                laserIndicator.enabled = false;
                return;
            }
        }
        crosshair.enabled = true;

        if (laserIsActive.activeSelf)
        {
            if (!laserIndicator.enabled)
                laserIndicator.enabled = true;
        }
        else
        {
            laserIndicator.enabled = false;
        }
    }
}
