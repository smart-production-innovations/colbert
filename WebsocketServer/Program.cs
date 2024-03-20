
byte[] cert = File.ReadAllBytes("cert.pfx");

WebsocketRelay relay = new WebsocketRelay("0.0.0.0", 443, true, cert, "");
relay.Start();
Console.WriteLine("server started");

Console.Read();

relay.Stop();
Console.WriteLine("server stopped");
Console.Read();