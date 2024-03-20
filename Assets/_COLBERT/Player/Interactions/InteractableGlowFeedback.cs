using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

//add hover glow effect to an interactable
public class InteractableGlowFeedback : MonoBehaviour
{
    private void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);
    }

    private void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelectEnter);
        interactable.selectExited.RemoveListener(OnSelectExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }
    
    private void OnSelectExit(SelectExitEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    public static void UpdateGlow(XRBaseInteractable interactable)
    {
        bool glow = false;

        //enable glow, if there is any interactor hovering (but not selecting!) this interactable
        foreach (var hoveringInteractor in interactable.interactorsHovering)
            if (!((IXRSelectInteractor)hoveringInteractor).IsSelecting(interactable))
                glow = true;

        if (glow)
            AddGlow(interactable);
        else
            RemoveGlow(interactable);
    }

    public static void AddGlow(XRBaseInteractable obj)
    {
        MeshRenderer[] meshrenderers = obj.transform.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                mat.SetInt("_GlowIsActive", 1);
            }
        }

        SkinnedMeshRenderer[] skinnedMesh = obj.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedMesh)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                mat.SetInt("_GlowIsActive", 1);
            }
        }
    }

    public static void RemoveGlow(XRBaseInteractable obj)
    {
        MeshRenderer[] meshrenderers = obj.transform.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                mat.SetInt("_GlowIsActive", 0);
            }
        }

        SkinnedMeshRenderer[] skinnedMesh = obj.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedMesh)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                mat.SetInt("_GlowIsActive", 0);
            }
        }
    }

    public static void ResumeGlow(XRBaseInteractable obj)
    {
        if (obj.isHovered)
            AddGlow(obj);
        else
            RemoveGlow(obj);
    }
}
