using System.IO;
using MaxLib.WebServer;
using MaxLib.WebServer.WebSocket;

namespace Websocket.Relay
{
    public class WebSocketEndpoint : WebSocketEndpoint<WebSocketConnection>
    {
        public override string? Protocol => null;

        private readonly EventFactory factory = new EventFactory();

        public WebSocketEndpoint()
        {
            // fill the factory with the known event types
            factory.Add<Events.Relay>();
        }

        protected override WebSocketConnection? CreateConnection(Stream stream, HttpRequestHeader header)
        {
            if (header.Location.DocumentPathTiles.Length != 1)
                return null;
            if (header.Location.DocumentPathTiles[0].ToLowerInvariant() != "ws")
                return null;
            if (!header.Location.GetParameter.TryGetValue("id", out string? id))
                return null;
            if (!ChannelGroup.Channels.TryGetValue(id, out ChannelGroup? group))
                return null;
            return new WebSocketConnection(
                group,
                stream,
                factory
            );
        }
    }
}
