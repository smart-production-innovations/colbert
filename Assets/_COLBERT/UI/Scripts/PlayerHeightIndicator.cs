using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

//show the height of the non-xr player when it changes
public class PlayerHeightIndicator : MonoBehaviour
{
    [SerializeField]
    private PlayerNonXr pcPlayer;

    [SerializeField]
    private InputActionReference heightAction;

    [SerializeField]
    private Canvas heightCanvas;

    private Transform cameraHeight;

    [SerializeField]
    private TextMeshProUGUI heightText;

    private double height; 

    void Update()
    {
        cameraHeight = pcPlayer.Camera.transform;
        if (heightAction.action.ReadValue<float>() == 1.0f)
        {
            height = cameraHeight.position.y + 0.2f;
            height = System.Math.Round(height, 2);
            heightText.text = "Player Height: " + height + " m";
            heightCanvas.enabled = true;
        }
        else if (heightCanvas.isActiveAndEnabled)
        {
            heightCanvas.enabled = false;
        }
    }
}
