using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//disable menu entries that should not be available depending on options set in BuildOptions component in scene
public class BuildOptionsTablet : MonoBehaviour
{
    [SerializeField]
    private Toggle modelsTabToggle;
    [SerializeField]
    private Toggle environmentsTabToggle;
    [SerializeField]
    private Toggle multiplayerTabToggle;
    [SerializeField]
    private UIModelsList modelsTab;
    [SerializeField]
    private UIEnvironmentList environmentsTab;
    [SerializeField]
    private UILobby lobbyTab;
    [SerializeField]
    private Button switchToHostButton;

    private void Awake()
    {
        BuildOptions options = FindAnyObjectByType<BuildOptions>();

        if (options.DisableMultiplayer)
        {
            multiplayerTabToggle.gameObject.SetActive(false);
            lobbyTab.gameObject.SetActive(false);
        }
        if (options.DisableMultiplayerHosting)
        {
            switchToHostButton.gameObject.SetActive(false);
            lobbyTab.SwitchToJoinLobby();
        }
        if (options.DisableModelLoading)
        {
            modelsTabToggle.gameObject.SetActive(false);
            modelsTab.gameObject.SetActive(false);
        }
        if (options.DisableEnvironmentChange)
        {
            environmentsTabToggle.gameObject.SetActive(false);
            environmentsTab.gameObject.SetActive(false);
        }
    }
}
