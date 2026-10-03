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
}
