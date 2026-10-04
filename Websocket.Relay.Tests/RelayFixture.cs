using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MaxLib.WebServer;

namespace Websocket.Relay.Tests;

[TestClass]
public static class RelayFixture
{
    private static Server? server;

    public static int Port { get; private set; }

    public static HttpClient Http { get; private set; } = null!;

    [AssemblyInitialize]
    public static void Start(TestContext _)
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        Port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();

        server = RelayServer.Create(Port);
        server.Start();
        Http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Port}/") };
    }

    [AssemblyCleanup]
    public static void Stop()
    {
        Http.Dispose();
        server?.Stop();
        (server as IDisposable)?.Dispose();
    }

    public static async Task<(string id, string token)> CreateGroupAsync(bool? replayLast = null)
    {
        var url = replayLast is null
            ? "api/new"
            : $"api/new?replay-last={replayLast.Value.ToString().ToLowerInvariant()}";
        var json = await Http.GetFromJsonAsync<JsonElement>(url).ConfigureAwait(false);
        return (json.GetProperty("id").GetString()!, json.GetProperty("token").GetString()!);
    }

    public static async Task<ClientWebSocket> ConnectAsync(string id)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(
            new Uri($"ws://127.0.0.1:{Port}/ws?id={Uri.EscapeDataString(id)}"),
            CancellationToken.None
        ).ConfigureAwait(false);
        return ws;
    }

    public static Task SendAsync(ClientWebSocket ws, object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        return ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public static async Task<JsonElement> ReceiveJsonAsync(ClientWebSocket ws)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[16 * 1024];
        using var ms = new MemoryStream();
        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, cts.Token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                Assert.Fail($"socket closed: {result.CloseStatus}");
            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                break;
        }
        return JsonDocument.Parse(Encoding.UTF8.GetString(ms.ToArray())).RootElement.Clone();
    }

    public static async Task Close(ClientWebSocket ws)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cts.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // the server may already have dropped the socket
        }
        ws.Dispose();
    }
}
