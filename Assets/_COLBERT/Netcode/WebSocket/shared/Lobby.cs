using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

//data of a lobby + serialization
public struct Lobby
{
    public int id;
    public string code;
    public string name;
    public bool isPrivate;
    public Dictionary<byte, LobbyMember> members;

    public Lobby(string code, string name, bool isPrivate)
    {
        this.code = code;
        this.id = IdFromCode(code);
        this.name = name;
        this.isPrivate = isPrivate;
        this.members = new Dictionary<byte, LobbyMember>();
    }

    public bool IsEmpty => members == null || members.Count == 0;
    public static Lobby Empty() => new Lobby();

    public LobbyMember LocalMember => members.First(member => member.Value.isLocal).Value;

    #region serialize/deserialize

    public void Serialize(byte[] buffer, ref int offset)
    {
        WebsocketMessage.SerializeString(code, buffer, ref offset);
        WebsocketMessage.SerializeString(name, buffer, ref offset);
        WebsocketMessage.SerializeByte((byte)members.Count, buffer, ref offset);
        foreach (var member in members)
            member.Value.Serialize(buffer, ref offset);
    }

    public void Deserialize(byte[] buffer, ref int offset)
    {
        isPrivate = false;
        code = WebsocketMessage.DeserializeString(buffer, ref offset);
        id = IdFromCode(code);
        name = WebsocketMessage.DeserializeString(buffer, ref offset);
        byte memberCount = WebsocketMessage.DeserializeByte(buffer, ref offset);

        if (members == null)
            members = new Dictionary<byte, LobbyMember>();
        else
            members.Clear();
        for (byte i = 0; i < memberCount; i++)
        {
            LobbyMember member = new LobbyMember();
            member.Deserialize(buffer, ref offset);
            members[member.clientId] = member;
        }
    }

    public int SerializedSize()
    {
        int size = 2 + code.Length + name.Length + 1; //code + name + membercount
        foreach (var member in members)
            size += member.Value.SerializedSize();
        return size;
    }

    #endregion

    #region serialize/deserialize light (without members)

    public void SerializeLight(byte[] buffer, ref int offset)
    {
        WebsocketMessage.SerializeString(code, buffer, ref offset);
        WebsocketMessage.SerializeString(name, buffer, ref offset);
        WebsocketMessage.SerializeByte((byte)members.Count, buffer, ref offset);
    }

    public void DeserializeLight(byte[] buffer, ref int offset)
    {
        isPrivate = false;
        code = WebsocketMessage.DeserializeString(buffer, ref offset);
        id = IdFromCode(code);
        name = WebsocketMessage.DeserializeString(buffer, ref offset);
        byte memberCount = WebsocketMessage.DeserializeByte(buffer, ref offset);

        if (members == null)
            members = new Dictionary<byte, LobbyMember>();
        else
            members.Clear();
        for (byte i = 0; i < memberCount; i++)
            members[i] = new LobbyMember();
    }

    public int SerializedSizeLight()
    {
        return 2 + code.Length + name.Length + 1; //code + name + membercount
    }

    #endregion

    #region serialize/deserialize lobbylist (light, without members)

    public static byte[] SerializeLobbies(IEnumerable lobbies)
    {
        int size = 0;
        foreach (Lobby lobby in lobbies)
            if (!lobby.isPrivate)
                size += lobby.SerializedSizeLight();

        byte[] bytes = new byte[size];
        int offset = 0;
        foreach (Lobby lobby in lobbies)
        {
            if (!lobby.isPrivate)
                lobby.SerializeLight(bytes, ref offset);
        }
        return bytes;
    }
    public static List<Lobby> DeserializeLobbies(byte[] bytes)
    {
        List<Lobby> lobbies = new List<Lobby>();
        if (bytes == null)
            return lobbies;
        int offset = 0;
        while (offset < bytes.Length)
        {
            Lobby lobby = new Lobby();
            lobby.DeserializeLight(bytes, ref offset);
            lobbies.Add(lobby);
        }
        return lobbies;
    }

    #endregion

    #region joincode

    public static int IdFromCode(string joincode)
    {
        return joincode.GetHashCode();
    }

    public static string RandomJoinCode()
    {
        return GetUniqueKey(4);
    }

    //https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
    public static string GetUniqueKey(int size)
    {
        byte[] data = new byte[4 * size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }
        StringBuilder result = new StringBuilder(size);
        for (int i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }

    #endregion
}
