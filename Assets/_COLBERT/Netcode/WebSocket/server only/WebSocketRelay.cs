using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebSocketSharp.Server;

public class WebsocketRelay
{
    private string listenAddress;
    private int port;
    private bool secure;
    private byte[] serverCertificate;
    private string certificatePassword;

    private string servicename = "relay";

    private HttpServer server = null;

    public WebsocketRelay(string listenAddress, int port, bool secure = false, byte[] serverCertificate = null, string certificatePassword = null)
    {
        this.listenAddress = listenAddress;
        this.port = port;
        this.secure = secure;
        this.serverCertificate = serverCertificate;
        this.certificatePassword = certificatePassword;
    }

    public void Start()
    {
        if (server == null)
        {
            server = new HttpServer(IPAddress.Parse(listenAddress), port, secure);
            server.OnGet += OnGet; //https request for lobbies list before connection through websocket

            server.AddWebSocketService<RelayBehaviour>($"/{servicename}"); //websocketservice for relay/lobbies management when connected
            if (secure)
            {
#if NET_STANDARD_2_1
                server.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12; //tls13 unsupported in .net standard
#else
                server.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
#endif
                server.SslConfiguration.ServerCertificate = new X509Certificate2(serverCertificate, certificatePassword);
            }

            server.Start();
        }
    }

    public void Stop()
    {
        server?.Stop();
        server = null;
    }

    private void OnGet(object sender, HttpRequestEventArgs e) //send lobbies list
    {
        var req = e.Request;
        if (req.RawUrl != "/lobbies")
            return;

        var res = e.Response;
        byte[] contents = Lobby.SerializeLobbies(RelayBehaviour.Lobbies);
        res.ContentType = "text/html";
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = contents.LongLength;
        res.Close(contents, true);
    }
}
