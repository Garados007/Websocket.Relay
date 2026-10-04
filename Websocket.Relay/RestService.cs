using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer;
using MaxLib.WebServer.Builder;

namespace Websocket.Relay
{
    public class RestService : Service
    {
        [Path("/api/new")]
        [Method(HttpProtocolMethod.Get)]
        public async Task<HttpDataSource> NewConnection(WebProgressTask task)
        {
            var replayLast = task.Request.Location.GetParameter.TryGetValue("replay-last", out var raw)
                && bool.TryParse(raw, out var value) && value;
            var group = ChannelGroup.AddGroup(replayLast);
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WriteString("id", group.Id);
            writer.WriteString("token", group.Token);
            writer.WriteEndObject();
            await writer.FlushAsync().ConfigureAwait(false);
            stream.Position = 0;

            return new HttpStreamDataSource(stream)
            {
                MimeType = MimeType.ApplicationJson,
            };
        }
    }
}
