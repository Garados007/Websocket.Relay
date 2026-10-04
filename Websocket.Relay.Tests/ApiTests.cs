using System.Net;
using System.Text.Json;

namespace Websocket.Relay.Tests;

[TestClass]
public class ApiTests
{
    [TestMethod]
    public async Task NewReturnsIdAndToken()
    {
        var response = await RelayFixture.Http.GetAsync("api/new");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(response.Content.Headers.ContentType?.ToString(), "application/json");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.IsFalse(string.IsNullOrEmpty(json.GetProperty("id").GetString()));
        Assert.IsFalse(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }

    [TestMethod]
    public async Task NewWithReplayLast()
    {
        var response = await RelayFixture.Http.GetAsync("api/new?replay-last=true");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task NewWithSuffixIsNotFound()
    {
        var response = await RelayFixture.Http.GetAsync("api/new/x");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CorsEchoesOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/new");
        request.Headers.Add("Origin", "http://example.test");
        var response = await RelayFixture.Http.SendAsync(request);
        Assert.AreEqual("http://example.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.IsTrue(response.Headers.GetValues("Vary").Any(x => x.Contains("Origin")));
    }

    [TestMethod]
    public async Task OptionsAnswersPreflightWithoutCreatingGroup()
    {
        var before = ChannelGroup.Channels.Keys.ToHashSet();
        using var request = new HttpRequestMessage(HttpMethod.Options, "api/new");
        request.Headers.Add("Origin", "http://example.test");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "x-test");
        var response = await RelayFixture.Http.SendAsync(request);
        Assert.IsTrue(response.IsSuccessStatusCode);
        Assert.AreEqual("http://example.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.AreEqual("GET", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.AreEqual("x-test", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
        Assert.IsFalse(ChannelGroup.Channels.Keys.Any(x => !before.Contains(x)), "a group was created");
    }

    [TestMethod]
    public async Task PostIsNotFoundWithoutCreatingGroup()
    {
        var before = ChannelGroup.Channels.Keys.ToHashSet();
        var response = await RelayFixture.Http.PostAsync("api/new", null);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.IsFalse(ChannelGroup.Channels.Keys.Any(x => !before.Contains(x)), "a group was created");
    }
}
