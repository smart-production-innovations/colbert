public enum WebSocketEvent
{
    Data = 0,
    Connect = 1,
    Disconnect = 2,
    TransportFailure = 3,
    Nothing = 4,
    ConnectServer = 5,
    ConnectClient = 6,
    ClientUnreachable = 7,
    LobbyUpdate = 9,
}