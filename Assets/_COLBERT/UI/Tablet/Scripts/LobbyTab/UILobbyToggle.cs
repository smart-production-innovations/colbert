using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

//disables the lobby tab button in the navbar, if any model is loaded to prevent starting multiplayer
public class UILobbyToggle : MonoBehaviour
{
    [SerializeField]
    private Toggle lobbyTab; //navbar button
    private NetworkModelsManager modelsManager = null;

    private void Awake()
    {
        modelsManager = FindAnyObjectByType<NetworkModelsManager>();
    }

    private void Update()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            bool isModelLoaded = modelsManager != null && modelsManager.LoadedModels.Count > 0;
            lobbyTab.interactable = !isModelLoaded;
        }
    }
}
