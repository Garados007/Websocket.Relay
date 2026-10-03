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

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }
}
