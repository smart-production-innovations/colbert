using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

//button for the controller menu (spherical buttons next to vr controller)
public class XRSphereButton : MonoBehaviour
{
    [SerializeField]
    private bool isToggleButton;
    [SerializeField]
    private GameObject sphere;

    public bool isActive = false;

    [SerializeField]
    private Color buttonColor;

    [SerializeField] 
    private Color selectColor;

    [SerializeField]
    private Vector3 sphereScale;

    public UnityEvent activateButton;
    public UnityEvent deactivateButton;

    [SerializeField]
    private bool activatesOnEnter;

    private bool wasPressed = false;

    public bool isHovered = false;

    private void Start()
    {
        sphereScale = sphere.transform.localScale;
        TintSelection(buttonColor);
    }
    public void OnButtonHoverEnter(HoverEnterEventArgs args)
    {
        sphere.transform.localScale = sphereScale * 1.1f;
        isHovered = true;
    }

    public void OnButtonHoverExit(HoverExitEventArgs args)
    {
        sphere.transform.localScale = sphereScale;
        wasPressed = false;
        isHovered = false;
    }

    public void OnSelectButtonEnter(SelectEnterEventArgs args)
    {
        sphere.transform.localScale = sphereScale;

        if (!isToggleButton) TintSelection(selectColor);
        if (activatesOnEnter) activateButton.Invoke();
    }

    public void OnSelectButtonExit(SelectExitEventArgs args)
    {
        sphere.transform.localScale = sphereScale;

        if (isToggleButton && !wasPressed)
        {
            if (isActive)
            {
                isActive = false;
                TintSelection(buttonColor);
                deactivateButton.Invoke();
                wasPressed = true;
            }
            else
            {
                isActive = true;
                TintSelection(selectColor);
                activateButton.Invoke();
                wasPressed = true;
            }
        }
        else
        {
            TintSelection(buttonColor);
            if (!activatesOnEnter) activateButton.Invoke();
        }
    }

    public void SetPassivActive()
    {
        isActive = true;
        TintSelection(selectColor);
    }
    public void SetPassivDeactive()
    {
        isActive = false;
        TintSelection(buttonColor);
    }

    public void TintSelection(Color color)
    {
        MeshRenderer[] meshrenderers = sphere.transform.GetComponentsInChildren<MeshRenderer>();
        if (meshrenderers.Length > 0)
        {
            foreach (MeshRenderer renderer in meshrenderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    mat.SetColor("_Color", color);
                }
            }
        }
    }

}
