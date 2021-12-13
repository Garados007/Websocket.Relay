using System.Text.Json;
using MaxLib.WebServer.WebSocket;

namespace Websocket.Relay.Events
{
    public class Relay : EventBase
    {
        public string? Token { get; private set; }

        public JsonElement Value { get; private set; }

        public override void ReadJsonContent(JsonElement json)
        {
            Token = json.TryGetProperty("token", out JsonElement node) ?
                node.GetString() : null;
            Value = json.TryGetProperty("value", out node) ? node : new JsonElement();
        }

        protected override void WriteJsonContent(Utf8JsonWriter writer)
        {
            writer.WritePropertyName("value");
            Value.WriteTo(writer);
        }
    }
}