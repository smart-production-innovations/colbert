using System.Collections;
using System.Collections.Generic;

//data of a lobbymember + serialization
public struct LobbyMember
{
    public byte clientId;
    public string name;
    public string ID; //only used on relay server
    public bool isLocal;

    public LobbyMember(byte clientId, string ID, string name)
    {
        this.clientId = clientId;
        this.ID = ID;
        this.name = name;
        this.isLocal = false;
    }

    #region serialize/deserialize

    public void Serialize(byte[] buffer, ref int offset)
    {
        WebsocketMessage.SerializeByte(clientId, buffer, ref offset);
        WebsocketMessage.SerializeString(name, buffer, ref offset);
    }

    public void Deserialize(byte[] buffer, ref int offset)
    {
        clientId = WebsocketMessage.DeserializeByte(buffer, ref offset);
        name = WebsocketMessage.DeserializeString(buffer, ref offset);
        ID = null;
        isLocal = false;
    }

    public int SerializedSize()
    {
        return 1 + 1 + name.Length; //clientId + name
    }

    #endregion
}
