using System.Net.WebSockets;
using static Websocket.Relay.Tests.RelayFixture;

namespace Websocket.Relay.Tests;

[TestClass]
public class MessageSizeTests
{
    [TestMethod]
    public void ParseDefault()
    {
        Assert.AreEqual(1048576, RelayServer.ParseMaxMessageSize(null));
        Assert.AreEqual(1048576, RelayServer.ParseMaxMessageSize(""));
    }

    [TestMethod]
    public void ParseValue()
    {
        Assert.AreEqual(2048, RelayServer.ParseMaxMessageSize("2048"));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("abc")]
    public void ParseInvalid(string value)
    {
        Assert.ThrowsExactly<ArgumentException>(() => RelayServer.ParseMaxMessageSize(value));
    }

    [TestMethod]
    public async Task MessageBelowLimitIsDelivered()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        var value = new string('a', 1_000_000);
        await SendAsync(ws, new Dictionary<string, object?> { ["$type"] = "Relay", ["token"] = token, ["value"] = value });
        var json = await ReceiveJsonAsync(ws);
        Assert.AreEqual(value, json.GetProperty("value").GetString());
        await Close(ws);
    }

    [TestMethod]
    public async Task MessageAboveLimitClosesSender()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        var value = new string('a', 1_100_000);
        await SendAsync(ws, new Dictionary<string, object?> { ["$type"] = "Relay", ["token"] = token, ["value"] = value });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[16 * 1024];
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, cts.Token);
        }
        while (result.MessageType != WebSocketMessageType.Close);
        Assert.AreEqual(WebSocketCloseStatus.MessageTooBig, result.CloseStatus);
        ws.Dispose();
    }
}
