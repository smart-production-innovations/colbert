using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEngine;
#endif
using WebSocketSharp;
using WebSocketSharp.Server;

//server logic for websocket communication
public class RelayBehaviour : WebSocketBehavior
{
    private static bool debugLog = false;

    private static Dictionary<int, Lobby> lobbies = new Dictionary<int, Lobby>();
    public static IEnumerable<Lobby> Lobbies => lobbies.Values;

    private static byte idCounter = 0;

    protected override void OnOpen()
    {
        LogAlways("Relay client connected");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        LogAlways("Relay client disconnected");

        CleanLobbies();
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        //Log($"Relay OnMessage");

        if (!e.IsBinary || e.RawData.Length < 2)
        {
            //LogWarning($"Relay OnMessage - received unexpected data");
            return;
        }

        WebsocketMessage.Deserialize(e.RawData, out byte toId, out WebSocketEvent type);

        switch (type)
        {
            case WebSocketEvent.ConnectServer:
                CleanLobbies();
                int offset = 2;
                bool isPrivate = WebsocketMessage.DeserializeBool(e.RawData, ref offset);
                string lobbyCode = WebsocketMessage.DeserializeString(e.RawData, ref offset);
                string lobbyName = WebsocketMessage.DeserializeString(e.RawData, ref offset);
                string memberName = WebsocketMessage.DeserializeString(e.RawData, ref offset);
                Lobby lobby = new Lobby(lobbyCode, lobbyName, isPrivate);
                if (lobbies.ContainsKey(lobby.id))
                {
                    LogWarning($"Relay OnMessage - could not create lobby - already exists");
                    return;
                }
                lobby.members[0] = new LobbyMember(0, ID, memberName);
                lobbies[lobby.id] = lobby;
                SendLobbyUpdate(lobby);
                return;
            case WebSocketEvent.ConnectClient:
                CleanLobbies();
                byte clientId = GetFreeClientId();
                if (clientId == 0)
                {
                    LogWarning($"Relay OnMessage - could not connect to relay (no free id)");
                    e.RawData[1] = (byte)WebSocketEvent.TransportFailure;
                    Send(e.RawData);
                    return;
                }
                offset = 2;
                lobbyCode = WebsocketMessage.DeserializeString(e.RawData, ref offset);
                int lobbyId = Lobby.IdFromCode(lobbyCode);
                if (!lobbies.ContainsKey(lobbyId))
                {
                    LogWarning($"Relay OnMessage - could not join lobby - lobby does not exist");
                    e.RawData[1] = (byte)WebSocketEvent.Disconnect;
                    Send(e.RawData);
                    return;
                }
                memberName = WebsocketMessage.DeserializeString(e.RawData, ref offset);
                lobbies[lobbyId].members[clientId] = new LobbyMember(clientId, ID, memberName);
                e.RawData[0] = clientId;
                e.RawData[1] = (byte)WebSocketEvent.Connect;
                SendLobbyUpdate(lobbies[lobbyId]);
                break;
        }

        if (!FindClient(ID, out byte fromId, out int fromLobby))
        {
            LogWarning($"Relay OnMessage - unknown sender - ignore");
            return;
        }

        if (!FindClient(toId, out string receiver, out int toLobby))
        {
            LogWarning($"Relay OnMessage - receiver {toId} not found - return disconnect message");

            Send(WebsocketMessage.Serialize(toId, WebSocketEvent.ClientUnreachable));
            return;
        }

        if (fromLobby != toLobby)
        {
            LogWarning($"Relay OnMessage - lobbies not matching {fromLobby} -> {toLobby} - ignore");
            return;
        }

        e.RawData[0] = fromId;

        Log($"Relay OnMessage - {type} from {fromId} to {toId} / {Sessions.Count} (receiver={receiver})");
        Sessions.SendTo(e.RawData, receiver);

        Log($"Relay OnMessage - end");
    }

    private void SendLobbyUpdate(Lobby lobby)
    {
        int size = 2 + lobby.SerializedSize();
        byte[] buffer = new byte[size];
        buffer[1] = (byte)WebSocketEvent.LobbyUpdate;
        int offset = 2;
        lobby.Serialize(buffer, ref offset);

        foreach (var member in lobby.members.Values)
        {
            buffer[0] = member.clientId;
            Sessions.SendTo(buffer, member.ID);
        }
    }

    //remove lobbies without active host
    private void CleanLobbies()
    {
        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies.ElementAt(i).Value;
            if (!Sessions.IDs.Contains(lobby.members[0].ID)) //check, if host connected
            {
                lobbies.Remove(lobby.id);
                i--;
            }
            else
            {
                if (CleanLobby(lobby))
                    SendLobbyUpdate(lobby);
            }
        }
    }
    private bool CleanLobby(Lobby lobby) //returns true, if lobby has changed
    {
        bool hasChanged = false;
        var members = lobby.members;
        for (int i = 0; i < members.Count; i++)
        {
            LobbyMember member = members.ElementAt(i).Value;
            if (!Sessions.IDs.Contains(member.ID)) //check, if host connected
            {
                hasChanged = true;
                members.Remove(member.clientId);
                i--;
            }
        }
        return hasChanged;
    }

    private byte GetFreeClientId()
    {
        for (int i = 0; i < byte.MaxValue; i++)
        {
            idCounter = (byte)((idCounter + 1) % byte.MaxValue);
            if (idCounter != 0 && IsClientIdFree(idCounter))
                return idCounter;
        }
        return 0;
    }

    private bool IsClientIdFree(byte clientId)
    {
        foreach (var lobby in lobbies)
        {
            foreach (var member in lobby.Value.members)
            {
                if (member.Key == clientId)
                    return false;
            }
        }
        return true;
    }

    private bool FindClient(string ID, out byte clientId, out int lobbyId)
    {
        foreach (var lobby in lobbies)
        {
            foreach (var member in lobby.Value.members)
            {
                if (member.Value.ID == ID)
                {
                    lobbyId = lobby.Key;
                    clientId = member.Key;
                    return true;
                }
            }
        }
        lobbyId = 0;
        clientId = 0;
        return false;
    }

    private bool FindClient(byte clientId, out string ID, out int lobbyId)
    {
        foreach (var lobby in lobbies)
        {
            foreach (var member in lobby.Value.members)
            {
                if (member.Key == clientId)
                {
                    lobbyId = lobby.Key;
                    ID = member.Value.ID;
                    return true;
                }
            }
        }
        lobbyId = 0;
        ID = null;
        return false;
    }

    private static void LogAlways(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#else
        Console.WriteLine(message);
#endif
    }

    private static void Log(string message)
    {
        if (debugLog)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }
    }

    private static void LogWarning(string message)
    {
        if (debugLog)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#else
            Console.WriteLine(message);
#endif
        }
    }
}
