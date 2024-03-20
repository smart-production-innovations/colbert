using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;

//manage local active players (xr and non-xr player)
//toggle players
//initialize xr when created
public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    protected PlayerXr playerXR;
    [SerializeField]
    protected PlayerNonXr playerNonXR;

    [SerializeField]
    private InputActionProperty spawnXRPlayer;
    [SerializeField]
    private InputActionProperty spawnNonXRPlayer;
    [SerializeField]
    protected Image selectedIndicatorXR;
    [SerializeField]
    protected Image selectedIndicatorNonXR;

    public PlayerXr PlayerXR => playerXR;
    public PlayerNonXr PlayerNonXR => playerNonXR;
    public bool XRPlayerActive => playerXR.isActiveAndEnabled;
    public bool NonXRPlayerActive => playerNonXR.isActiveAndEnabled;


    private void Awake()
    {
        if (spawnXRPlayer != null)
            spawnXRPlayer.action.performed += (_) => ToggleXRPlayer();
        if (spawnNonXRPlayer != null)
            spawnNonXRPlayer.action.performed += (_) => ToggleNonXRPlayer();
    }

    private void Start()
    {
        if (NonXRPlayerActive)
            ToggleNonXRPlayer(true);
        if (XRPlayerActive)
            ToggleXRPlayer(true);
    }

    private void OnDestroy()
    {
        StopXR();
    }

    public void ToggleXRPlayer()
    {
        ToggleXRPlayer(!XRPlayerActive);
    }

    public void ToggleNonXRPlayer()
    {
        ToggleNonXRPlayer(!NonXRPlayerActive);
    }

    public void ToggleXRPlayer(bool enable)
    {
        playerXR.gameObject.SetActive(enable);
        if (selectedIndicatorXR != null)
            selectedIndicatorXR.enabled = enable;

        UpdateAudioListener();
        SetTeleportProvider();

        if (enable)
            StartXR();
        else
            StopXR();
    }

    public void ToggleNonXRPlayer(bool enable)
    {
        playerNonXR.gameObject.SetActive(enable);
        if (selectedIndicatorNonXR != null)
            selectedIndicatorNonXR.enabled = enable;

        UpdateAudioListener();
    }


    //update to which player the audiolistener is attached to (prioritize xr player)
    private void UpdateAudioListener()
    {
        if (XRPlayerActive)
        {
            if (playerNonXR.Camera.TryGetComponent(out AudioListener listener))
                DestroyImmediate(listener);

            if (!playerXR.Camera.TryGetComponent(out AudioListener _))
                playerXR.Camera.gameObject.AddComponent<AudioListener>();
        }
        else if (NonXRPlayerActive)
        {
            if (playerXR.Camera.TryGetComponent(out AudioListener listener))
                DestroyImmediate(listener);

            if (!playerNonXR.Camera.TryGetComponent(out AudioListener _))
                playerNonXR.Camera.gameObject.AddComponent<AudioListener>();
        }
    }

    public void SetTeleportProvider()
    {
        TeleportationArea[] areas = FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TeleportationProvider provider = playerXR.GetComponent<TeleportationProvider>();
        foreach (TeleportationArea area in areas)
            area.teleportationProvider = provider;
    }

    #region initialize xr

    private Coroutine initializeXRCoroutine = null;
    private int targetFramerate = -1;
    private int vsyncCount = -1;

    private void StartXR()
    {
        if (initializeXRCoroutine != null)
            return;

        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("XR not supported?");
            return;
        }

        initializeXRCoroutine = StartCoroutine(StartXRCoroutine());
    }

    private IEnumerator StartXRCoroutine()
    {
        yield return null;

        targetFramerate = Application.targetFrameRate;
        vsyncCount = QualitySettings.vSyncCount;

        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            ToggleXRPlayer(false);
            ToggleNonXRPlayer(true);
        }
        else
        {
            Debug.Log($"Starting XR with Active Loader: '{XRGeneralSettings.Instance.Manager.activeLoader.name}' and with runtime '{UnityEngine.XR.OpenXR.OpenXRRuntime.name}'");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        initializeXRCoroutine = null;
    }

    private void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)// && XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            //XRGeneralSettings.Instance.Manager.StopSubsystems(); //called automatically by DeinitializeLoader
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR stopped completely.");

            //restore values because stopping xr overrides vsync and framerate settings
            if (targetFramerate != -1)
                Application.targetFrameRate = targetFramerate;
            if (vsyncCount != -1)
                QualitySettings.vSyncCount = vsyncCount;
        }
    }

    #endregion
}
