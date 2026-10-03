using System;
using System.Globalization;
using MaxLib.WebServer;
using MaxLib.WebServer.Builder;
using MaxLib.WebServer.Services;

namespace Websocket.Relay
{
    public static class RelayServer
    {
        public const long DefaultMaxMessageSize = 1024 * 1024;

        public static long ParseMaxMessageSize(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return DefaultMaxMessageSize;
            if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var size) && size > 0)
                return size;
            throw new ArgumentException(
                $"RELAY_MAX_MESSAGE_SIZE must be a positive integer (bytes), but was '{value}'.");
        }

        public static Server Create(int port, long maxMessageSize = DefaultMaxMessageSize)
        {
            var server = new Server(new WebServerSettings(port, 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());
            server.AddWebService(Service.Build<RestService>()!);
            server.AddWebService(new CorsService());

            var ws = new MaxLib.WebServer.WebSocket.WebSocketService();
            ws.Add(new WebSocketEndpoint(maxMessageSize));
            server.AddWebService(ws);

            return server;
        }
    }
}
