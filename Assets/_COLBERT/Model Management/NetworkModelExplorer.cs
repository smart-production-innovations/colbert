using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

//handles assembling/disassembling of one model
[RequireComponent(typeof(NetworkModel))]
public class NetworkModelExplorer : NetworkBehaviour
{
    [SerializeField]
    private InputActionProperty assembleNonXR;
    [SerializeField]
    private InputActionProperty disassembleNonXR;

    [SerializeField]
    private InputActionProperty assembleXRLeft;
    [SerializeField]
    private InputActionProperty disassembleXRLeft;
    [SerializeField]
    private InputActionProperty assembleXRRight;
    [SerializeField]
    private InputActionProperty disassembleXRRight;

    private PlayerManager playerManager = null;


    private void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
    }

    private void OnEnable()
    {
        assembleNonXR.action.performed += AssembleNonXr;
        disassembleNonXR.action.performed += DisassembleNonXr;

        assembleXRLeft.action.performed += AssembleXr;
        disassembleXRLeft.action.performed += DisassembleXr;
        assembleXRRight.action.performed += AssembleXr;
        disassembleXRRight.action.performed += DisassembleXr;
    }

    private void OnDisable()
    {
        assembleNonXR.action.performed -= AssembleNonXr;
        disassembleNonXR.action.performed -= DisassembleNonXr;

        assembleXRLeft.action.performed -= AssembleXr;
        disassembleXRLeft.action.performed -= DisassembleXr;
        assembleXRRight.action.performed += AssembleXr;
        disassembleXRRight.action.performed += DisassembleXr;
    }

    #region input action callbacks

    private void AssembleNonXr(InputAction.CallbackContext _)
    {
        Assemble(playerManager.PlayerNonXR);
    }

    private void DisassembleNonXr(InputAction.CallbackContext _)
    {
        Disassemble(playerManager.PlayerNonXR);
    }

    private void AssembleXr(InputAction.CallbackContext _)
    {
        Assemble(playerManager.PlayerXR);
    }

    private void DisassembleXr(InputAction.CallbackContext _)
    {
        Disassemble(playerManager.PlayerXR);
    }

    #endregion

    #region dis-/assemble possible

    public static bool AssemblePossible(Player player)
    {
        NetworkModelInteractable interactable = GetActiveInteractable(player);
        if (interactable == null || !interactable.Initialized)
            return false;

        NetworkModelExplorer explorer = interactable.Model.GetComponent<NetworkModelExplorer>();
        return explorer.AssemblePossible(interactable);
    }
    public static bool DisassemblePossible(Player player)
    {
        NetworkModelInteractable interactable = GetActiveInteractable(player);
        if (interactable == null || !interactable.Initialized)
            return false;

        NetworkModelExplorer explorer = interactable.Model.GetComponent<NetworkModelExplorer>();
        return explorer.DisassemblePossible(interactable);
    }

    public bool AssemblePossible(NetworkModelInteractable interactable) //possible, if interactable is not the root node
    {
        return interactable != null && interactable.Node != interactable.Model.RootNode;
    }
    public bool DisassemblePossible(NetworkModelInteractable interactable) //possible, if there is a branch in the interactable's children
    {
        if (interactable == null)
            return false;

        List<ModelNode> children = interactable.Node.Children;
        while (children.Count == 1)
            children = children[0].Children;

        return children.Count != 0;
    }

    #endregion

    #region assemble/disassemble

    public void Assemble(Player player)
    {
        Assemble(GetActiveInteractable(player));
    }

    public void Disassemble(Player player)
    {
        Disassemble(GetActiveInteractable(player));
    }

    public void Assemble(NetworkModelInteractable interactable)
    {
        if (interactable == null)
            return;

        if (IsSpawned && !IsServer)
        {
            AssembleServerRpc(new NetworkObjectReference(interactable.NetworkObject));
            return;
        }

        if (!AssemblePossible(interactable))
            return;

        ModelNode parent = interactable.Node.Parent;

        //remove child interactables
        NetworkModel model = interactable.Model;
        foreach (ModelNode child in parent.Children)
        {
            NetworkModelInteractable childInteractable = child.GetComponentInParent<NetworkModelInteractable>();
            if (childInteractable)
                NetworkModelInteractable.Delete(childInteractable, true);
        }

        //determine new interactable node (parent)
        while (parent.Parent != null && parent.Parent.Children.Count == 1)
            parent = parent.Parent;

        //create new interactable
        NetworkModelInteractable.Create(model, parent, model.InteractablePrefab);
    }

    public void Disassemble(NetworkModelInteractable interactable)
    {
        if (interactable == null)
            return;

        if (IsSpawned && !IsServer)
        {
            DisassembleServerRpc(new NetworkObjectReference(interactable.NetworkObject));
            return;
        }

        if (!DisassemblePossible(interactable))
            return;

        //determine children (new interactables after disassemble)
        List<ModelNode> children = interactable.Node.Children;
        while (children.Count == 1)
            children = children[0].Children;

        //create new interactables
        interactable.gameObject.SetActive(false); //disable the parent XRGrabInteractable, so the colliders it uses are unregistered in the XRInteractionManager and available for the children
        NetworkModel model = interactable.Model;
        foreach (ModelNode child in children)
        {
            NetworkModelInteractable.Create(model, child, model.InteractablePrefab);
        }

        //delete old parent interactable
        NetworkModelInteractable.Delete(interactable, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AssembleServerRpc(NetworkObjectReference interactableReference)
    {
        if (!interactableReference.TryGet(out NetworkObject no))
            return;
        NetworkModelInteractable interactable = no.GetComponent<NetworkModelInteractable>();
        Assemble(interactable);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisassembleServerRpc(NetworkObjectReference interactableReference)
    {
        if (!interactableReference.TryGet(out NetworkObject no))
            return;
        NetworkModelInteractable interactable = no.GetComponent<NetworkModelInteractable>();
        Disassemble(interactable);
    }

    #endregion

    #region static helper

    public static NetworkModelInteractable GetActiveInteractable(Player player)
    {
        if (!player.IsActive)
            return null;

        XRBaseInteractor[] interactors = player.GetComponentsInChildren<XRBaseInteractor>();
        foreach (XRBaseInteractor interactor in interactors)
        {
            List<IXRHoverInteractable> interactables = interactor.interactablesHovered;
            if (interactables == null || interactables.Count == 0)
                continue;

            foreach (IXRHoverInteractable interactable in interactables)
            {
                if (interactable.transform.TryGetComponent(out NetworkModelInteractable helper))
                {
                    return helper;
                }
            }
        }
        return null;
    }

    #endregion

}
