using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//customize available features in build, values are used in BuildOptionsTablet components on players
public class BuildOptions : MonoBehaviour
{
    [SerializeField]
    [Tooltip("if checked: cannot use multiplayer")]
    private bool disableMultiplayer = false;

    [SerializeField]
    [Tooltip("if checked: cannot create host multiplayer session (host lobbies)")]
    private bool disableMultiplayerHosting = false;

    [SerializeField]
    [Tooltip("if checked: cannot load models")]
    private bool disableModelLoading = false;

    [SerializeField]
    [Tooltip("if checked: cannot change environment")]
    private bool disableEnvironmentChange = false;

    [SerializeField]
    [Tooltip("Use this to set framerate to screen refreshrate (default framerate on android is 30) (affects android builds only)")]
    private bool setAndroidFramerateToRefreshrate = true;

    public bool DisableMultiplayer => disableMultiplayer;
    public bool DisableMultiplayerHosting => disableMultiplayerHosting;
    public bool DisableModelLoading => disableModelLoading;
    public bool DisableEnvironmentChange => disableEnvironmentChange;


    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (setAndroidFramerateToRefreshrate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        }
#endif
    }
}
