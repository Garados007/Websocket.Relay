using System.Net.WebSockets;
using static Websocket.Relay.Tests.RelayFixture;

namespace Websocket.Relay.Tests;

[TestClass]
public class RelayTests
{
    private static object Msg(string? token, object? value) =>
        new Dictionary<string, object?> { ["$type"] = "Relay", ["token"] = token, ["value"] = value };

    private static async Task AssertNextValue(ClientWebSocket ws, string expected)
    {
        var json = await ReceiveJsonAsync(ws);
        Assert.AreEqual(expected, json.GetProperty("value").GetString());
    }

    [TestMethod]
    public async Task RelayReachesAllWithoutToken()
    {
        var (id, token) = await CreateGroupAsync();
        var r1 = await ConnectAsync(id);
        var r2 = await ConnectAsync(id);
        var sender = await ConnectAsync(id);
        await SendAsync(sender, Msg(token, "hello"));
        foreach (var ws in new[] { r1, r2, sender })
        {
            var json = await ReceiveJsonAsync(ws);
            Assert.AreEqual("Relay", json.GetProperty("$type").GetString());
            Assert.AreEqual("hello", json.GetProperty("value").GetString());
            Assert.IsFalse(json.TryGetProperty("token", out _));
        }
        foreach (var ws in new[] { r1, r2, sender })
            await Close(ws);
    }

    [TestMethod]
    public async Task WrongTokenIsDropped()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        await SendAsync(ws, Msg("wrong", "bad"));
        await Task.Delay(100);
        await SendAsync(ws, Msg(token, "sentinel"));
        await AssertNextValue(ws, "sentinel");
        await Close(ws);
    }

    [TestMethod]
    public async Task MissingTokenIsDropped()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        await SendAsync(ws, new Dictionary<string, object?> { ["$type"] = "Relay", ["value"] = "bad" });
        await Task.Delay(100);
        await SendAsync(ws, Msg(token, "sentinel"));
        await AssertNextValue(ws, "sentinel");
        await Close(ws);
    }

    [TestMethod]
    public async Task RapidSecondMessageIsDropped()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        await SendAsync(ws, Msg(token, "first"));
        await SendAsync(ws, Msg(token, "second"));
        await AssertNextValue(ws, "first");
        await Task.Delay(100);
        await SendAsync(ws, Msg(token, "sentinel"));
        await AssertNextValue(ws, "sentinel");
        await Close(ws);
    }

    [TestMethod]
    public async Task ReplayLastDeliversToLateJoiner()
    {
        var (id, token) = await CreateGroupAsync(true);
        var ws = await ConnectAsync(id);
        await SendAsync(ws, Msg(token, "last"));
        await AssertNextValue(ws, "last");
        var late = await ConnectAsync(id);
        var json = await ReceiveJsonAsync(late);
        Assert.AreEqual("last", json.GetProperty("value").GetString());
        Assert.IsFalse(json.TryGetProperty("token", out _));
        await Close(ws);
        await Close(late);
    }

    [TestMethod]
    public async Task WithoutReplayLateJoinerGetsNothing()
    {
        var (id, token) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        await SendAsync(ws, Msg(token, "old"));
        await AssertNextValue(ws, "old");
        var late = await ConnectAsync(id);
        await Task.Delay(100);
        await SendAsync(ws, Msg(token, "sentinel"));
        await AssertNextValue(late, "sentinel");
        await Close(ws);
        await Close(late);
    }

    [TestMethod]
    public async Task UnknownGroupIsRejected()
    {
        await Assert.ThrowsExactlyAsync<WebSocketException>(() => ConnectAsync("does-not-exist"));
    }

    [TestMethod]
    [Ignore("MaxLib 3.0 never reports closed sockets to the group; re-enabled with the 5.0 upgrade.")]
    public async Task GroupIsRemovedWhenEmpty()
    {
        var (id, _) = await CreateGroupAsync();
        var ws = await ConnectAsync(id);
        await Close(ws);
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (ChannelGroup.Channels.ContainsKey(id))
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail("group still exists");
            await Task.Delay(50);
        }
        await Assert.ThrowsExactlyAsync<WebSocketException>(() => ConnectAsync(id));
    }
}
