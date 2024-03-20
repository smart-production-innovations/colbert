using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

//one instance for every client 
//manages remote player versions of non-owned players
//sync vr/pc player state to this script, if owner OR sync state from this script to remote player versions, if not owner
public class NetworkPlayer : NetworkBehaviour
{
    [Header("player prefabs")]
    [SerializeField]
    private PlayerXrNet playerPrefabXR; //remote player
    [SerializeField]
    private PlayerNonXrNet playerPrefabNonXR; //remote player

    [Header("xr transforms")]
    [SerializeField]
    private NetworkTransform headTransformXR;
    [SerializeField]
    private NetworkTransform leftLaserTransformXR;
    [SerializeField]
    private NetworkTransform rightLaserTransformXR;
    [SerializeField]
    private NetworkTransform leftControllerTransformXR;
    [SerializeField]
    private NetworkTransform rightControllerTransformXR;

    [Header("nonxr transforms")]
    [SerializeField]
    private NetworkTransform headTransformNonXR;
    [SerializeField]
    private NetworkTransform laserTransformNonXR;

    private NetworkVariable<bool> xr = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> nonxr = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> xrLaserLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> xrLaserRight = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> nonxrLaser = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<short> clientId = new NetworkVariable<short>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkString> controllerNameLeft = new NetworkVariable<NetworkString>(NetworkString.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkString> controllerNameRight = new NetworkVariable<NetworkString>(NetworkString.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private PlayerManager playerManager = null;
    private PlayerXrNet playerXR = null;
    private PlayerNonXrNet playerNonXR = null;
    private WebSocketTransport transport = null;


    public byte ClientId => (byte)clientId.Value;


    public override void OnNetworkSpawn()
    {
        name = $"NetworkPlayer {OwnerClientId} {(IsOwner ? "local" : "remote")}";

        UpdateActivePlayers();
        UpdateTransforms();
    }

    public override void OnNetworkDespawn()
    {
        DestroyXR();
        DestroyNonXR();
    }

    private void Awake()
    {
        transport = FindAnyObjectByType<WebSocketTransport>();
        playerManager = FindAnyObjectByType<PlayerManager>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        DestroyXR();
        DestroyNonXR();
    }

    private void Update()
    {
        UpdateActivePlayers();
        UpdateTransforms();
        UpdateController();
    }


    //update which player is active (xr / non-xr) and create/destroy the corresponding remote player objects
    private void UpdateActivePlayers()
    {
        if (IsOwner)
        {
            xr.Value = playerManager.XRPlayerActive;
            nonxr.Value = playerManager.NonXRPlayerActive;

            xrLaserLeft.Value = xr.Value && playerManager.PlayerXR.Left.GetComponentInChildren<Laserpointer>().isActive;
            xrLaserRight.Value = xr.Value && playerManager.PlayerXR.Right.GetComponentInChildren<Laserpointer>().isActive;
            nonxrLaser.Value = nonxr.Value && playerManager.PlayerNonXR.GetComponentInChildren<Laserpointer>().isActive;

            if (!transport.Lobby.IsEmpty)
                clientId.Value = transport.Lobby.LocalMember.clientId;
        }
        else
        {
            if (nonxr.Value && playerNonXR == null)
                CreateNonXR();
            else if (!nonxr.Value && playerNonXR != null)
                DestroyNonXR();

            if (xr.Value && playerXR == null)
                CreateXR();
            else if (!xr.Value && playerXR != null)
                DestroyXR();

            if (xr.Value)
            {
                playerXR.Left.GetComponentInChildren<Laserpointer>(true).SetActive(xrLaserLeft.Value);
                playerXR.Right.GetComponentInChildren<Laserpointer>(true).SetActive(xrLaserRight.Value);
            }

            if (nonxr.Value)
                playerNonXR.GetComponentInChildren<Laserpointer>(true).SetActive(nonxrLaser.Value);
        }
    }

    //update poses of players (head, controller, laser)
    private void UpdateTransforms()
    {
        if (IsOwner)
        {
            if (playerManager.NonXRPlayerActive)
            {
                Camera cam = playerManager.PlayerNonXR.Camera;
                headTransformNonXR.transform.position = cam.transform.position;
                headTransformNonXR.transform.rotation = cam.transform.rotation;
                laserTransformNonXR.transform.position = cam.transform.position;
                laserTransformNonXR.transform.rotation = cam.transform.rotation;
            }
            if (playerManager.XRPlayerActive)
            {
                Camera cam = playerManager.PlayerXR.Camera;
                headTransformXR.transform.position = cam.transform.position;
                headTransformXR.transform.rotation = cam.transform.rotation;
                leftLaserTransformXR.transform.position = playerManager.PlayerXR.Left.GetComponentInChildren<Laserpointer>(true).transform.position;
                leftLaserTransformXR.transform.rotation = playerManager.PlayerXR.Left.GetComponentInChildren<Laserpointer>(true).transform.rotation;
                rightLaserTransformXR.transform.position = playerManager.PlayerXR.Right.GetComponentInChildren<Laserpointer>(true).transform.position;
                rightLaserTransformXR.transform.rotation = playerManager.PlayerXR.Right.GetComponentInChildren<Laserpointer>(true).transform.rotation;

                ActionBasedControllerManager[] controllerManagers = playerManager.PlayerXR.GetComponentsInChildren<ActionBasedControllerManager>(true);
                foreach (var controllerManager in controllerManagers)
                {
                    if (controllerManager.HandNode == ActionBasedControllerManager.Hand.LeftHand)
                    {
                        ActionBasedController controller = controllerManager.GetComponentInChildren<ActionBasedController>(true);
                        leftControllerTransformXR.transform.position = controller.transform.position;
                        leftControllerTransformXR.transform.rotation = controller.transform.rotation;
                        break;
                    }
                }
                foreach (var controllerManager in controllerManagers)
                {
                    if (controllerManager.HandNode == ActionBasedControllerManager.Hand.RightHand)
                    {
                        ActionBasedController controller = controllerManager.GetComponentInChildren<ActionBasedController>(true);
                        rightControllerTransformXR.transform.position = controller.transform.position;
                        rightControllerTransformXR.transform.rotation = controller.transform.rotation;
                        break;
                    }
                }

            }
        }
        else
        {
            if (playerNonXR != null)
            {
                Camera cam = playerNonXR.GetComponentInChildren<Camera>(true);
                cam.transform.position = headTransformNonXR.transform.position;
                cam.transform.rotation = headTransformNonXR.transform.rotation;
                playerNonXR.GetComponentInChildren<Laserpointer>(true).transform.position = laserTransformNonXR.transform.position;
                playerNonXR.GetComponentInChildren<Laserpointer>(true).transform.rotation = laserTransformNonXR.transform.rotation;
            }
            if (playerXR != null)
            {
                Camera cam = playerXR.GetComponentInChildren<Camera>(true);
                cam.transform.position = headTransformXR.transform.position;
                cam.transform.rotation = headTransformXR.transform.rotation;
                playerXR.Left.GetComponentInChildren<Laserpointer>(true).transform.position = leftLaserTransformXR.transform.position;
                playerXR.Left.GetComponentInChildren<Laserpointer>(true).transform.rotation = leftLaserTransformXR.transform.rotation;
                playerXR.Right.GetComponentInChildren<Laserpointer>(true).transform.position = rightLaserTransformXR.transform.position;
                playerXR.Right.GetComponentInChildren<Laserpointer>(true).transform.rotation = rightLaserTransformXR.transform.rotation;

                PickRightModel[] models = playerXR.GetComponentsInChildren<PickRightModel>(true);
                foreach (var model in models)
                {
                    if (model.handedness == PickRightModel.Handedness.Left)
                    {
                        Transform controller = model.transform.parent;
                        controller.transform.position = leftControllerTransformXR.transform.position;
                        controller.transform.rotation = leftControllerTransformXR.transform.rotation;
                        break;
                    }
                }
                foreach (var model in models)
                {
                    if (model.handedness == PickRightModel.Handedness.Right)
                    {
                        Transform controller = model.transform.parent;
                        controller.transform.position = rightControllerTransformXR.transform.position;
                        controller.transform.rotation = rightControllerTransformXR.transform.rotation;
                        break;
                    }
                }

            }
        }
    }

    //update controller model to show the actual controller of the remote player
    private void UpdateController()
    {
        if (IsOwner)
        {
            if (playerManager.XRPlayerActive)
            {
                PickRightModel[] models = playerManager.PlayerXR.GetComponentsInChildren<PickRightModel>(true);
                foreach (var model in models)
                {
                    switch (model.handedness)
                    {
                        case PickRightModel.Handedness.Left:
                            if (controllerNameLeft.Value.Value != model.ActiveControllerName)
                                controllerNameLeft.Value = new NetworkString(model.ActiveControllerName);
                            break;
                        case PickRightModel.Handedness.Right:
                            if (controllerNameRight.Value.Value != model.ActiveControllerName)
                                controllerNameRight.Value = new NetworkString(model.ActiveControllerName);
                            break;
                    }
                }
            }
        }
        else
        {
            if (playerXR != null)
            {
                PickRightModel[] models = playerXR.GetComponentsInChildren<PickRightModel>(true);
                foreach (var model in models)
                {
                    switch (model.handedness)
                    {
                        case PickRightModel.Handedness.Left:
                            model.SetModelByName(controllerNameLeft.Value.Value);
                            break;
                        case PickRightModel.Handedness.Right:
                            model.SetModelByName(controllerNameRight.Value.Value);
                            break;
                    }
                }
            }
        }
    }

    #region create/destroy playerprefabs

    private void CreateXR()
    {
        if (playerXR != null)
            return;

        playerXR = Instantiate(playerPrefabXR);
        playerXR.name = $"{playerPrefabXR.name} {OwnerClientId} remote";

        var hud = playerXR.GetComponentInChildren<PlayerNameLabel>();
        hud?.OnNameChange(transport.Lobby.members[ClientId].name);
    }

    private void CreateNonXR()
    {
        if (playerNonXR != null)
            return;

        playerNonXR = Instantiate(playerPrefabNonXR);
        playerNonXR.name = $"{playerPrefabNonXR.name} {OwnerClientId} remote";

        var hud = playerNonXR.GetComponentInChildren<PlayerNameLabel>();
        hud?.OnNameChange(transport.Lobby.members[ClientId].name);
    }

    private void DestroyXR()
    {
        if (playerXR == null)
            return;

        Destroy(playerXR.gameObject);
        playerXR = null;
    }

    private void DestroyNonXR()
    {
        if (playerNonXR == null)
            return;

        Destroy(playerNonXR.gameObject);
        playerNonXR = null;
    }

    #endregion

}
