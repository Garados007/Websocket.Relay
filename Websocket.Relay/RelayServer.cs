using MaxLib.WebServer;
using MaxLib.WebServer.Services;

namespace Websocket.Relay
{
    public static class RelayServer
    {
        public static Server Create(int port)
        {
            var server = new Server(new WebServerSettings(port, 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());
            server.AddWebService(new RestService().BuildService());
            server.AddWebService(new CorsService());

            var ws = new MaxLib.WebServer.WebSocket.WebSocketService();
            ws.Add(new WebSocketEndpoint());
            server.AddWebService(ws);

            return server;
        }
    }
}
