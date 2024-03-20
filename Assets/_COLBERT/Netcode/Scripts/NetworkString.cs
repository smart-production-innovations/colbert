using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct NetworkString : INetworkSerializable
{
    private string value;
    public string Value
    {
        get { return value; }
        set { this.value = value != null ? value : ""; }
    }

    public static NetworkString Empty = new NetworkString("");

    public NetworkString(string value)
    {
        this.value = value != null ? value : "";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out value);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(value);
        }
    }
}
