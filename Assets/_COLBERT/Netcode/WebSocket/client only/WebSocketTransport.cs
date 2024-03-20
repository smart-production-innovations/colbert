using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WebSocketSharp;

//custom networktransport for communication through custom websocketserver
public class WebSocketTransport : Unity.Netcode.NetworkTransport
{
    [Header("Server Config")]
    [SerializeField]
    public string connectAddress = "127.0.0.1";
    [SerializeField]
    private ushort port = 443;

    [Header("Proxy Config")]
    [SerializeField]
    private string proxyUrl = null;
    [SerializeField]
    private string proxyUsername = null;
    [SerializeField]
    private string proxyPassword = null;

    [Header("Other")]
    [SerializeField]
    private bool secure = true;
    [SerializeField]
    [Tooltip("is the lobby visible for everyone or only with the correct lobbyCode")]
    private bool lobbyPrivate = false;
    [SerializeField]
    [Tooltip("start a server instance to host a local lobby")]
    private bool hostServer = false;
    [SerializeField]
    private RelayServer relayServer = null;
    [SerializeField]
    private string servicename = "relay";
    [SerializeField]
    private bool debugLog = false;

    [Header("Lobby Data")]
    [Tooltip("the code to join or create a lobby")]
    public string lobbyCode;
    [SerializeField]
    [Tooltip("the default name of the lobby to create (host only)")]
    private string lobbyName;
    [SerializeField]
    [Tooltip("the default player name to join with")]
    private string lobbyMemberName;

    public UnityEvent<List<Lobby>> lobbyListEvent;
    public UnityEvent<Lobby> lobbyChangedEvent;

    public delegate void MessageReceivedDelegate(WebsocketMessage msg);
    public MessageReceivedDelegate onMessageReceived;

    private bool lobbyChanged = false;

    private WebSocket client = null;
    private ConcurrentQueue<WebsocketMessage> receiveQueue = new ConcurrentQueue<WebsocketMessage>();
    private bool isServer = false;
    private bool isClient = false;
    private Lobby lobby;
    public Lobby Lobby => lobby;

    private string lobbyNameKey = "lobbyNameKey";
    private string lobbyMemberNameKey = "lobbyMemberNameKey";
    private string lobbyPrivateKey = "lobbyPivateKey";
    private string hostServerKey = "hostServerKey";

    public string LobbyName
    {
        get { return lobbyName; }
        set
        {
            if (value == null || string.IsNullOrWhiteSpace(value))
                return;

            lobbyName = value;
            PlayerPrefs.SetString(lobbyNameKey, lobbyName);
        }
    }
    public string LobbyMemberName
    {
        get { return lobbyMemberName; }
        set
        {
            if (value == null || string.IsNullOrWhiteSpace(value))
                return;

            lobbyMemberName = value;
            PlayerPrefs.SetString(lobbyMemberNameKey, lobbyMemberName);
        }
    }
    public bool LobbyPrivate
    {
        get { return lobbyPrivate; }
        set
        {
            lobbyPrivate = value;
            PlayerPrefs.SetInt(lobbyPrivateKey, lobbyPrivate ? 1 : 0);
        }
    }
    public bool HostServer
    {
        get { return hostServer; }
        set
        {
            hostServer = value;
            PlayerPrefs.SetInt(hostServerKey, hostServer ? 1 : 0);
        }
    }

    private void Awake()
    {
        LoadConfigs();

        if (PlayerPrefs.HasKey(lobbyNameKey))
            lobbyName = PlayerPrefs.GetString(lobbyNameKey);

        if (PlayerPrefs.HasKey(lobbyMemberNameKey))
            lobbyMemberName = PlayerPrefs.GetString(lobbyMemberNameKey);

        if (PlayerPrefs.HasKey(lobbyPrivateKey))
            lobbyPrivate = PlayerPrefs.GetInt(lobbyPrivateKey) != 0;

        if (PlayerPrefs.HasKey(hostServerKey))
            hostServer = PlayerPrefs.GetInt(hostServerKey) != 0;
    }

    private void Update()
    {
        if (lobbyChanged)
        {
            lobbyChanged = false;
            lobbyChangedEvent?.Invoke(lobby);
        }
    }

    private void PingLoop()
    {
        Task.Run(async () =>
        {
            while (client != null)
            {
                if (!client.IsAlive)
                {
                    DisconnectLocalClient();
                    return;
                }
                await Task.Delay(5000);
            }
        });
    }

    public void SendMessage(byte[] data)
    {
        if (client != null)
            client?.Send(data);
    }

    #region Netcode.NetworkTransport implementations

    public override ulong ServerClientId => 0;

    public override void DisconnectLocalClient()
    {
        if (debugLog) Debug.Log($"*DisconnectLocalClient");

        if (client != null)
        {
            bool isConnected = client.IsAlive;
            if (isConnected)
                client.Send(WebsocketMessage.Serialize((byte)ServerClientId, WebSocketEvent.Disconnect));
        }

        client?.Close();
        client = null;
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        if (debugLog) Debug.Log($"*DisconnectRemoteClient {clientId}");

        if (client != null)
        {
            bool isConnected = client.IsAlive;
            if (isConnected)
                client.Send(WebsocketMessage.Serialize((byte)clientId, WebSocketEvent.Disconnect));
        }
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        return 0;
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
        if (debugLog) Debug.Log($"*Initialize");

        receiveQueue.Clear();
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        NetworkEvent type;
        receiveTime = Time.realtimeSinceStartup;

        if (receiveQueue.TryDequeue(out WebsocketMessage msg))
        {
            clientId = msg.clientId;
            type = NetcodeEvent(msg.type);
            payload = msg.ArraySegment();
            if (debugLog) Debug.Log($"poll {type} , {clientId}");
        }
        else
        {
            clientId = 0;
            type = NetworkEvent.Nothing;
            payload = new ArraySegment<byte>();
        }

        return type;
    }

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        if (debugLog) Debug.Log($"*Send {clientId}");
        client.Send(WebsocketMessage.Serialize((byte)clientId, WebSocketEvent.Data, payload));
    }

    public override void Shutdown()
    {
        if (debugLog) Debug.Log($"*Shutdown");

        client?.Close();
        client = null;

        isServer = false;
        isClient = false;

        if (hostServer) relayServer.enabled = false;
        if (debugLog) Debug.Log($"*Shutdown end");

        lobby = new Lobby();
        lobbyChangedEvent?.Invoke(lobby);
    }

    public override bool StartClient()
    {
        if (debugLog) Debug.Log($"*StartClient");
        isClient = true;
        StartWebSocketClient();
        return true;
    }

    public override bool StartServer()
    {
        if (debugLog) Debug.Log($"*StartServer");
        if (hostServer) relayServer.enabled = true;
        isServer = true;
        StartWebSocketClient();
        return true;
    }

    #endregion

    #region websocket callbacks

    private void OnOpen(object sender, EventArgs e)
    {
        if (debugLog) Debug.Log($"OnOpen");

        if (isServer)
        {
            client.Send(WebsocketMessage.SerializeConnect((byte)ServerClientId, WebSocketEvent.ConnectServer, lobbyCode, lobbyName, lobbyMemberName, lobbyPrivate));
        }
        else
        {
            client.Send(WebsocketMessage.SerializeConnect((byte)ServerClientId, WebSocketEvent.ConnectClient, lobbyCode, lobbyMemberName));
        }

        PingLoop();
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        if (debugLog) Debug.Log($"OnClose");

        if (isClient)
        {
            receiveQueue.Enqueue(new WebsocketMessage(WebSocketEvent.Disconnect));
            client.Send(WebsocketMessage.Serialize((byte)ServerClientId, WebSocketEvent.Disconnect));
        }
        else
        {
            receiveQueue.Enqueue(new WebsocketMessage(WebSocketEvent.TransportFailure));
        }
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        if (!e.IsBinary)
        {
            if (debugLog) Debug.LogWarning($"OnMessage - received unexpected data (non-binary)");
            return;
        }

        byte[] data = e.RawData;
        if (data.Length < 2)
        {
            if (debugLog) Debug.LogWarning($"OnMessage - received unexpected data (<2 bytes)");
            return;
        }

        WebsocketMessage msg = new WebsocketMessage(data);
        if (debugLog) Debug.Log($"OnMessage - {msg.type} from {msg.clientId}");

        switch (msg.type)
        {
            case WebSocketEvent.Connect:
                if (isServer)
                    client.Send(WebsocketMessage.Serialize(msg.clientId, msg.type));
                break;
            case WebSocketEvent.ClientUnreachable:
                receiveQueue.Enqueue(new WebsocketMessage(WebSocketEvent.Disconnect, msg.clientId));
                return;
            case WebSocketEvent.LobbyUpdate:
                int offset = 2;
                lobby.Deserialize(data, ref offset);
                LobbyMember member = lobby.members[msg.clientId];
                member.isLocal = true;
                lobby.members[msg.clientId] = member;
                lobbyChanged = true;
                PrintLobby(lobby);
                return;
        }

        onMessageReceived?.Invoke(msg);
        receiveQueue.Enqueue(msg);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"OnError '{e.Message}'");
        receiveQueue.Enqueue(new WebsocketMessage(WebSocketEvent.TransportFailure));
    }

    #endregion

    private void StartWebSocketClient()
    {
        if (client != null)
        {
            if (debugLog) Debug.LogWarning("could not start websocket client, already started");
            return;
        }

        string serverAddress = hostServer && isServer ? "127.0.0.1" : connectAddress;

        client = new WebSocket($"{(secure ? "wss" : "ws")}://{serverAddress}:{port}/{servicename}");
        client.OnMessage += OnMessage;
        client.OnOpen += OnOpen;
        client.OnError += OnError;
        client.OnClose += OnClose;


        if (!string.IsNullOrEmpty(proxyUrl))
        {
            Debug.Log($"use proxy from config file '{proxyUrl}'");
            client.SetProxy(proxyUrl, proxyUsername, proxyPassword);
        }
        else
        {
            var proxy = (WebProxy)WebRequest.GetSystemWebProxy();
            if (proxy != null && proxy.Address != null)
            {
                Debug.Log($"use system web proxy '{proxy.Address.AbsoluteUri}'");
                client.SetProxy(proxy.Address.AbsoluteUri, null, null);
            }
        }

        client.ConnectAsync();
    }

    //translate WebSocketEvent to Netcode.NetworkEvent
    private static NetworkEvent NetcodeEvent(WebSocketEvent type)
    {
        switch (type)
        {
            case WebSocketEvent.Data:
                return NetworkEvent.Data;
            case WebSocketEvent.Connect:
                return NetworkEvent.Connect;
            case WebSocketEvent.Disconnect:
                return NetworkEvent.Disconnect;
            case WebSocketEvent.TransportFailure:
                return NetworkEvent.TransportFailure;
            case WebSocketEvent.Nothing:
            default:
                return NetworkEvent.Nothing;
        }
    }

    public void RequestLobbies()
    {
        StartCoroutine(RequestLobbiesCoroutine(connectAddress));
    }

    private IEnumerator RequestLobbiesCoroutine(string uri)
    {
        uri = $"{(secure ? "https" : "http")}://{uri}/lobbies"; //?type={lobbies};

        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.certificateHandler = new AcceptCertificateAlways();

            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError(pages[page] + ": ConnectionError: " + webRequest.error);
                    OnLobbies(null);
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": DataProcessingError: " + webRequest.error);
                    OnLobbies(null);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": ProtocolError: " + webRequest.error);
                    OnLobbies(null);
                    break;
                case UnityWebRequest.Result.Success:
                    OnLobbies(webRequest.downloadHandler.data);
                    break;
            }
        }
    }

    private void OnLobbies(byte[] bytes)
    {
        List<Lobby> lobbies;
        if (bytes == null)
        {
            lobbies = new List<Lobby>();
        }
        else
        {
            lobbies = Lobby.DeserializeLobbies(bytes);

            foreach (Lobby lobby in lobbies)
                Debug.Log($"({lobby.id}) Lobby '{lobby.name}' with {lobby.members.Count} members");
        }
        lobbyListEvent?.Invoke(lobbies);
    }

    public static void PrintLobby(Lobby lobby)
    {
        Debug.Log($"Lobby '{lobby.name}' with code '{lobby.code}':");
        if (lobby.members == null)
        {
            Debug.Log($"no members");
        }
        else
        {
            foreach (LobbyMember member in lobby.members.Values)
            {
                Debug.Log($"Member '{member.name}' with id {member.clientId}");
            }
        }
    }


    private void LoadConfigs()
    {
        Dictionary<string, string> variables = ConfigHelper.ReadVariables(ConfigHelper.proxyConfigFile);
        if (variables != null)
        {
            if (variables.TryGetValue("url", out string value))
            {
                proxyUrl = value;
            }
            if (variables.TryGetValue("username", out value))
            {
                proxyUsername = value;
            }
            if (variables.TryGetValue("password", out value))
            {
                proxyPassword = value;
            }
            Debug.Log($"loaded proxy config with url='{proxyUrl}', username='{proxyUsername}' and password='{proxyPassword}'");
        }
        else
        {
            Debug.Log($"could not load proxy config from file '{ConfigHelper.proxyConfigFile}' (file not found)");
        }


        variables = ConfigHelper.ReadVariables(ConfigHelper.serverConfigFile);
        if (variables != null)
        {
            if (variables.TryGetValue("ip", out string value))
            {
                connectAddress = value;
            }
            if (variables.TryGetValue("port", out value))
            {
                port = ushort.Parse(value);
            }
            Debug.Log($"loaded server config with ip='{connectAddress}' and port='{port}'");
        }
        else
        {
            Debug.Log($"could not load server config from file '{ConfigHelper.serverConfigFile}' (file not found)");
        }
    }

}
