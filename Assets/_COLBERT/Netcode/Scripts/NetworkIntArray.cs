using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkIntArray : INetworkSerializable
{
    public int[] value;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
            serializer.GetFastBufferWriter().WriteValueSafe(value);
        else
            serializer.GetFastBufferReader().ReadValueSafe(out value);
    }
}