using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer;
using MaxLib.WebServer.Api.Rest;

namespace Websocket.Relay
{
    public class RestService
    {
        public RestApiService BuildService()
        {
            var api = new RestApiService("api");
            var fact = new ApiRuleFactory();
            api.RestEndpoints.AddRange(new[]
            {
                RestActionEndpoint.Create(NewConnection)
                    .Add(fact.Location(
                        fact.UrlConstant("new"),
                        fact.MaxLength()
                    )),
            });
            return api;
        }

        private async Task<HttpDataSource> NewConnection()
        {
            var group = ChannelGroup.AddGroup();
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WriteString("id", group.Id);
            writer.WriteString("token", group.Token);
            writer.WriteEndObject();
            await writer.FlushAsync().ConfigureAwait(false);

            return new HttpStreamDataSource(stream)
            {
                MimeType = MimeType.ApplicationJson,
            };
        }
    }
}