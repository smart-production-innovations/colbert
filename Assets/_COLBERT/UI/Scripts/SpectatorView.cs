using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

//spectate other players from their point of view (xr/non-xr, local/remote players)
public class SpectatorView : MonoBehaviour
{
    [SerializeField]
    private PlayerManager playerManager;

    [SerializeField]
    private TextMeshProUGUI username;

    [SerializeField]
    private InputActionProperty toggleSpectator;
    [SerializeField]
    private InputActionProperty switchSpectator;

    [SerializeField]
    private Canvas[] onScreenUI;

    [SerializeField]
    private Canvas[] spectatorUI;

    [SerializeField]
    private GameObject laserselectedIndicator;

    [SerializeField]
    private GameObject[] spectatorUIObjects;

    public bool isSpectator = false; //toggles spectator view

    private Player spectatedPlayer = null;


    private void OnEnable()
    {
        toggleSpectator.action.Enable();
        toggleSpectator.action.performed += ToggleSpectator;

        switchSpectator.action.Enable();
        switchSpectator.action.performed += SwitchSpectator;
    }

    private void OnDisable()
    {
        toggleSpectator.action.performed -= ToggleSpectator;
        toggleSpectator.action.Disable();

        switchSpectator.action.performed -= SwitchSpectator;
        switchSpectator.action.Disable();
    }

    private void Update()
    {
        if (CanSpectate())
        {
            foreach (var spectatorObject in spectatorUIObjects)
                if (!spectatorObject.activeSelf)
                    spectatorObject.SetActive(true);
        }
        else
        {
            foreach (var spectatorObject in spectatorUIObjects)
                if (spectatorObject.activeSelf)
                    spectatorObject.SetActive(false);
        }
        if (isSpectator && (spectatedPlayer == null || !spectatedPlayer.isActiveAndEnabled))
        {
            DisableSpectatorMode();
        }
    }

    private void ToggleSpectator(InputAction.CallbackContext obj)
    {
        if (isSpectator)
        {
            DisableSpectatorMode();
        }
        else //!isSpectator
        {
            if (CanSpectate())
            {
                SpectatePlayer(null);
            }
        }
    }

    private void SwitchSpectator(InputAction.CallbackContext obj)
    {
        if (!isSpectator)
            return;

        int index = Player.players.IndexOf(spectatedPlayer);
        index = (index + 1) % Player.players.Count;
        SpectatePlayer(Player.players[index]);
    }

    private bool CanSpectate()
    {
        return playerManager.PlayerNonXR.IsActive && Player.players.Count > 1;
    }

    private void SpectatePlayer(Player player)
    {
        if (laserselectedIndicator)
            laserselectedIndicator.SetActive(false);
        isSpectator = true;

        foreach (Canvas obj in onScreenUI)
        {
            obj.enabled = false;
        }
        foreach (Canvas obj in spectatorUI)
        {
            obj.enabled = true;
        }

        playerManager.ToggleNonXRPlayer(false);

        for (int i = 0; i < Player.players.Count; i++)
        {
            if (!Player.players[i].IsLocal)
            {
                Player.players[i].Camera.enabled = false;
                Player.players[i].Camera.stereoTargetEye = StereoTargetEyeMask.None;
                Player.players[i].Camera.fieldOfView = 60;
            }
        }

        if (player == null)
            player = Player.players[0];

        player.Camera.enabled = true;
        if (!player.IsLocal)
            player.Camera.stereoTargetEye = StereoTargetEyeMask.None;
        player.Camera.depth = 0;

        if (player.GetComponentInChildren<PlayerNameLabel>())
        {
            if (player.GetType() == typeof(PlayerXrNet)) username.text = "Currently spectating: VR User-" + player.GetComponentInChildren<PlayerNameLabel>().username.text;
            if (player.GetType() == typeof(PlayerNonXrNet)) username.text = "Currently spectating: PC User-" + player.GetComponentInChildren<PlayerNameLabel>().username.text;
        }
        else
        {
            username.text = "Currently spectating: " + "Own VR Player";
        }

        spectatedPlayer = player;
    }

    void DisableSpectatorMode()
    {
        isSpectator = false;

        foreach (Canvas obj in onScreenUI)
        {
            obj.enabled = true;
        }
        foreach (Canvas obj in spectatorUI)
        {
            obj.enabled = false;
        }

        for (int i = 0; i < Player.players.Count; i++)
        {
            Player.players[i].Camera.enabled = false;
        }

        playerManager.ToggleNonXRPlayer(true);
        playerManager.PlayerXR.Camera.enabled = true;
        playerManager.PlayerNonXR.Camera.enabled = true;

        spectatedPlayer = null;
    }
}
