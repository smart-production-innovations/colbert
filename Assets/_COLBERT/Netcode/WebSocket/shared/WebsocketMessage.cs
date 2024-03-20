using System;
using System.Text;

//a message in websocket communication + serialization
//a message is >= 2 bytes:
//  1st byte: clientId
//  2nd byte: type
//  3rd - x bytes: optional, arbitrary payload data
public struct WebsocketMessage
{
    public byte[] data;
    public byte clientId;
    public WebSocketEvent type;

    public WebsocketMessage(byte[] data)
    {
        this.data = data;
        this.clientId = data[0];
        this.type = (WebSocketEvent)data[1];
    }
    public WebsocketMessage(WebSocketEvent type, byte clientId)
    {
        this.data = null;
        this.clientId = clientId;
        this.type = type;
    }
    public WebsocketMessage(WebSocketEvent type)
    {
        this.data = null;
        this.clientId = 0;
        this.type = type;
    }

    public ArraySegment<byte> ArraySegment()
    {
        ArraySegment<byte> segment;
        if (data != null && data.Length > 2)
            segment = new ArraySegment<byte>(data, 2, data.Length - 2);
        else
            segment = new ArraySegment<byte>();
        return segment;
    }

    #region static serialize/deserialize helpers

    public static byte[] Serialize(byte clientId, WebSocketEvent type)
    {
        byte[] buffer = new byte[2];
        buffer[0] = clientId;
        buffer[1] = (byte)type;
        return buffer;
    }

    public static byte[] Serialize(byte clientId, WebSocketEvent type, byte[] payload)
    {
        byte[] buffer = new byte[2 + payload.Length];
        buffer[0] = clientId;
        buffer[1] = (byte)type;
        Buffer.BlockCopy(payload, 0, buffer, 2, payload.Length);
        return buffer;
    }

    public static byte[] Serialize(byte clientId, WebSocketEvent type, ArraySegment<byte> payload)
    {
        byte[] buffer = new byte[2 + payload.Count];
        buffer[0] = clientId;
        buffer[1] = (byte)type;
        Buffer.BlockCopy(payload.Array, payload.Offset, buffer, 2, payload.Count);
        return buffer;
    }

    public static byte[] SerializeConnect(byte clientId, WebSocketEvent type, string lobbyCode, string lobbyName, string memberName, bool isPrivate)//server - creates a lobby
    {
        byte[] buffer = new byte[3 + 3 * sizeof(int) + lobbyCode.Length + lobbyName.Length + memberName.Length];
        int offset = 0;
        SerializeByte(clientId, buffer, ref offset);
        SerializeByte((byte)type, buffer, ref offset);
        SerializeBool(isPrivate, buffer, ref offset);
        SerializeString(lobbyCode, buffer, ref offset);
        SerializeString(lobbyName, buffer, ref offset);
        SerializeString(memberName, buffer, ref offset);
        return buffer;
    }

    public static byte[] SerializeConnect(byte clientId, WebSocketEvent type, string lobbyCode, string memberName)//client - join a lobby
    {
        byte[] buffer = new byte[2 + 2 * sizeof(int) + lobbyCode.Length + memberName.Length];
        int offset = 0;
        SerializeByte(clientId, buffer, ref offset);
        SerializeByte((byte)type, buffer, ref offset);
        SerializeString(lobbyCode, buffer, ref offset);
        SerializeString(memberName, buffer, ref offset);
        return buffer;
    }

    public static void Deserialize(byte[] data, out byte clientId, out WebSocketEvent type)
    {
        clientId = data[0];
        type = (WebSocketEvent)data[1];
    }

    //copy unmanaged variable into byte array just like in BitConverter.GetBytes()
    public unsafe static void CopyBytes<T>(T value, byte[] dest, int offset) where T : unmanaged
    {
        fixed (byte* ptr = dest)
        {
            *(T*)(ptr + offset) = value;
        }
    }

    public static void SerializeByte(byte b, byte[] buffer, ref int offset)
    {
        buffer[offset] = b;
        offset++;
    }

    public static byte DeserializeByte(byte[] buffer, ref int offset)
    {
        byte b = buffer[offset];
        offset++;
        return b;
    }

    public static void SerializeBool(bool b, byte[] buffer, ref int offset)
    {
        CopyBytes(b, buffer, offset);
        offset += sizeof(bool);
    }
    public static bool DeserializeBool(byte[] buffer, ref int offset)
    {
        bool b = BitConverter.ToBoolean(buffer, offset);
        offset += sizeof(bool);
        return b;
    }

    public static void SerializeInt(int i, byte[] buffer, ref int offset)
    {
        CopyBytes(i, buffer, offset);
        offset += sizeof(int);
    }

    public static int DeserializeInt(byte[] buffer, ref int offset)
    {
        int i = BitConverter.ToInt32(buffer, offset);
        offset += sizeof(int);
        return i;
    }

    public static void SerializeString(string s, byte[] buffer, ref int offset)
    {
        buffer[offset] = (byte)s.Length;
        offset++;
        Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, offset);
        offset += s.Length;
    }

    public static string DeserializeString(byte[] buffer, ref int offset)
    {
        int length = buffer[offset];
        offset++;
        string s = Encoding.UTF8.GetString(buffer, offset, length);
        offset += length;
        return s;
    }

    #endregion
}