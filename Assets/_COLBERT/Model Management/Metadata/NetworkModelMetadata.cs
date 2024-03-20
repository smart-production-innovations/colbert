using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

//manages the infopanels for showing hierarchy and metadata infos
public class NetworkModelMetadata : NetworkBehaviour
{
    [SerializeField]
    private NetworkMetadataPanel panelPrefab;

    [SerializeField]
    private InputActionProperty showPanelNonXR;
    [SerializeField]
    private InputActionProperty showPanelXR;

    private List<NetworkMetadataPanel> panels = new List<NetworkMetadataPanel>();
    private PlayerManager playerManager = null;

    public List<NetworkMetadataPanel> Panels => panels;

    private void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
    }

    private void OnDisable()
    {
        if (IsServer || !IsSpawned)
            while (panels.Count > 0)
                RemovePanel(panels[0]);
    }

    private void Update()
    {
        if (showPanelNonXR.action.WasPressedThisFrame())
        {
            CreatePanelNonXR();
        }
        else if (showPanelNonXR.action.WasReleasedThisFrame())
        {
            RemovePanelNonXR();
        }

        if (showPanelXR.action.WasPressedThisFrame())
        {
            CreatePanelXR();
        }
        else if (showPanelXR.action.WasReleasedThisFrame())
        {
            RemovePanelXR();
        }
    }

    #region create panel

    private void CreatePanelXR()
    {
        foreach (var panel in panels)
        {
            if (panel.XR && (!IsSpawned || (IsSpawned && panel.IsOwner))) //don't create a new one, if the same user already has a panel open
                return;
        }

        CreatePanel(playerManager.PlayerXR, true);
    }

    private void CreatePanelNonXR()
    {
        foreach (var panel in panels)
        {
            if (!panel.XR && (!IsSpawned || (IsSpawned && panel.IsOwner))) //don't create a new one, if the same user already has a panel open
                return;
        }

        CreatePanel(playerManager.PlayerNonXR, false);
    }

    private void CreatePanel(Player player, bool xr)
    {
        NetworkModelInteractable interactable = NetworkModelExplorer.GetActiveInteractable(player);
        if (interactable == null || interactable.Node == null)
            return;

        PanelPose(player, interactable, out Vector3 position, out Quaternion rotation);

        if (IsSpawned && IsClient && !IsServer)
        {
            CreatePanelServerRpc(NetworkManager.LocalClientId, new NetworkObjectReference(interactable.NetworkObject), position, rotation, xr);
            return;
        }

        CreatePanel(interactable, position, rotation, xr, NetworkManager.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreatePanelServerRpc(ulong clientId, NetworkObjectReference interactableReference, Vector3 position, Quaternion rotation, bool xr)
    {
        if (!interactableReference.TryGet(out NetworkObject no))
            return;
        CreatePanel(no.GetComponent<NetworkModelInteractable>(), position, rotation, xr, clientId);
    }

    private NetworkMetadataPanel CreatePanel(NetworkModelInteractable interactable, Vector3 position, Quaternion rotation, bool xr, ulong clientId)
    {
        NetworkMetadataPanel panel = Instantiate(panelPrefab, position, rotation);
        panel.Initialize(interactable, this, xr);
        if (IsSpawned && IsServer)
            panel.NetworkObject.SpawnWithOwnership(clientId);
        return panel;
    }

    #endregion

    #region remove panel

    private void RemovePanelXR()
    {
        NetworkMetadataPanel panelToRemove = null;
        foreach (var panel in panels)
        {
            if (panel.XR && (!IsSpawned || (IsSpawned && panel.IsOwner)))
            {
                panelToRemove = panel;
                break;
            }
        }

        if (panelToRemove == null)
            return;

        RemovePanel(panelToRemove);
    }

    private void RemovePanelNonXR()
    {
        NetworkMetadataPanel panelToRemove = null;
        foreach (var panel in panels)
        {
            if (!panel.XR && (!IsSpawned || (IsSpawned && panel.IsOwner)))
            {
                panelToRemove = panel;
                break;
            }
        }

        if (panelToRemove == null)
            return;

        RemovePanel(panelToRemove);
    }

    private void RemovePanel(NetworkMetadataPanel panel)
    {
        if (IsSpawned && IsClient && !IsServer)
        {
            RemovePanelServerRpc(new NetworkObjectReference(panel.NetworkObject));
            return;
        }

        if (panel.IsSpawned)
            panel.NetworkObject.Despawn();
        Destroy(panel.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemovePanelServerRpc(NetworkObjectReference panelReference)
    {
        if (!panelReference.TryGet(out NetworkObject no))
            return;
        RemovePanel(no.GetComponent<NetworkMetadataPanel>());
    }

    #endregion

    #region static helper

    //calculate position for the panel in front of player
    private static void PanelPose(Player player, NetworkModelInteractable interactable, out Vector3 position, out Quaternion rotation)
    {
        Camera cam = player.GetComponentInChildren<Camera>();
        position = cam.transform.position + 0.45f * cam.transform.forward;
        XRDirectInteractor[] interactors = player.GetComponentsInChildren<XRDirectInteractor>();
        foreach (XRDirectInteractor interactor in interactors)
        {
            XRGrabInteractable i = interactable.GetComponent<XRGrabInteractable>();
            if (!interactor.IsHovering(i) && !interactor.IsSelecting(i))
            {
                position = interactor.transform.position + 0.3f * cam.transform.up;
                break;
            }
        }
        Vector3 forward = (position - cam.transform.position).normalized;
        rotation = Quaternion.LookRotation(forward);
    }

    #endregion
}
