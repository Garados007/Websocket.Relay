using System.Text.Json;
using System.Text.Json.Serialization;
using MaxLib.WebServer.WebSocket;

namespace Websocket.Relay.Events
{
    public class Relay : EventBase
    {
        [JsonPropertyName("token")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }

        private static readonly JsonElement Null = JsonDocument.Parse("null").RootElement;

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; } = Null;
    }
}
