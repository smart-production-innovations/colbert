using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;

//manages the whole lobby ui - switching panels for joining/hosting, all inputs/parameters...
public class UILobby : MonoBehaviour
{
    private WebSocketTransport transport;

    [Header("join/create")]
    [SerializeField]
    private RectTransform joinCreatePanel;
    [SerializeField]
    private TMP_InputField serverAddressInput;
    [SerializeField]
    private TMP_InputField usernameInput;

    [Header("create")]
    [SerializeField]
    private RectTransform createContent;
    [SerializeField]
    private TMP_InputField createLobbyNameInput;
    [SerializeField]
    private Toggle privateToggle;
    [SerializeField]
    private Toggle localToggle;

    [Header("join")]
    [SerializeField]
    private RectTransform joinContent;
    [SerializeField]
    private UILobbyList lobbyList;
    [SerializeField]
    private TMP_InputField joinCodeInput;

    [Header("inlobby")]
    [SerializeField]
    private RectTransform inlobbyPanel;
    [SerializeField]
    private TextMeshProUGUI lobbyName;
    [SerializeField]
    private TextMeshProUGUI lobbyCode;
    [SerializeField]
    private UILobbyMemberList lobbyMemberList;
    [SerializeField]
    private TextMeshProUGUI serverAddress;

    [SerializeField]
    private Image selectionJoin;
    [SerializeField]
    private Image selectionCreate;

    [SerializeField]
    private Loading loadingScreen;

    private WebSocketTransport Transport
    {
        get
        {
            if (transport == null)
                transport = FindAnyObjectByType<WebSocketTransport>();
            return transport;
        }
    }


    private void Start()
    {
        string username = Transport.LobbyMemberName;
        usernameInput.text = username;

        createLobbyNameInput.text = Transport.LobbyName;
        privateToggle.isOn = Transport.LobbyPrivate;
        localToggle.isOn = Transport.HostServer;

        Transport.lobbyChangedEvent.AddListener(OnLobbyUpdated);
        Transport.lobbyListEvent.AddListener(OnLobbyListUpdated);

        OnLobbyUpdated(Transport.Lobby);
    }

    private void OnDestroy()
    {
        if (Transport == null)
            return;

        Transport.lobbyChangedEvent.RemoveListener(OnLobbyUpdated);
        Transport.lobbyListEvent.RemoveListener(OnLobbyListUpdated);
    }

    public void EditName()
    {
        Transport.LobbyMemberName = usernameInput.text;
        usernameInput.text = Transport.LobbyMemberName;
    }

    public void EditServerAddress()
    {
        Transport.connectAddress = serverAddressInput.text;
        serverAddressInput.text = Transport.connectAddress; //show validated address in inputfield

        Debug.Log($"changed server address to '{Transport.connectAddress}'");

        RefreshLobbies();
    }

    public void EditLocal()
    {
        bool hostingServer = createContent.gameObject.activeSelf && localToggle.isOn;
        if (hostingServer)
        {
            serverAddressInput.text = "-";
            serverAddressInput.interactable = false;
        }
        else
        {
            serverAddressInput.text = Transport.connectAddress;
            serverAddressInput.interactable = true;
        }
    }

    public void SwitchToJoinLobby()
    {
        createContent.gameObject.SetActive(false);
        joinContent.gameObject.SetActive(true);
        selectionJoin.enabled = true;
        selectionCreate.enabled = false;
        EditLocal();
    }

    public void SwitchToCreateLobby()
    {
        createContent.gameObject.SetActive(true);
        joinContent.gameObject.SetActive(false);
        selectionJoin.enabled = false;
        selectionCreate.enabled = true;
        EditLocal();
    }

    public void CreateLobby()
    {
        string lobbyName = createLobbyNameInput.text;
        bool islocal = localToggle.isOn;
        bool isprivate = privateToggle.isOn;

        Transport.lobbyCode = Lobby.RandomJoinCode();
        Transport.LobbyName = lobbyName;
        Transport.LobbyPrivate = isprivate;
        Transport.HostServer = islocal;
        NetworkManager.Singleton.StartHost();
    }

    public void RefreshLobbies()
    {
        loadingScreen.EnableLoading();
        Transport.RequestLobbies();
    }

    public void JoinByCode()
    {
        string code = joinCodeInput.text;
        Transport.lobbyCode = code;
        NetworkManager.Singleton.StartClient();
    }

    public void JoinLobby(Lobby lobby)
    {
        Transport.lobbyCode = lobby.code;
        NetworkManager.Singleton.StartClient();
    }

    public void LeaveLobby()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public void OnLobbyListUpdated(List<Lobby> lobbies)
    {
        loadingScreen.DisableLoading();
        lobbyList.UpdateList(lobbies);
    }

    public void OnLobbyUpdated(Lobby lobby)
    {
        if (lobby.IsEmpty) //show join/create panel
        {
            RefreshLobbies();
            inlobbyPanel.gameObject.SetActive(false);
            SwitchToJoinLobby();
            joinCreatePanel.gameObject.SetActive(true);
        }
        else //show in-lobby panel
        {
            joinCreatePanel.gameObject.SetActive(false);
            inlobbyPanel.gameObject.SetActive(true);

            lobbyName.text = lobby.name;
            lobbyCode.text = lobby.code;

            if (Transport.HostServer)
            {
                IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                string text = "";
                foreach (IPAddress ip in ips)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        text += $"{ip}\n";
                    }
                }
                text.TrimEnd('\n');

                serverAddress.text = text;
            }
            else
            {
                serverAddress.text = Transport.connectAddress;
            }
        }
        lobbyMemberList.UpdateList(lobby);
    }

}
