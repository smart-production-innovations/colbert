using UnityEngine;

//run server inside application for local multiplayer
public class RelayServer : MonoBehaviour
{
    public string listenAddress = "0.0.0.0";
    public ushort port = 443;
    public bool secure = true;
    public TextAsset serverCertificate;
    public string certPassword;

    private WebsocketRelay relay = null;


    private void OnEnable()
    {
        relay = new WebsocketRelay(listenAddress, port, secure, serverCertificate.bytes, certPassword);
        relay.Start();
    }

    private void OnDisable()
    {
        relay.Stop();
    }
}
