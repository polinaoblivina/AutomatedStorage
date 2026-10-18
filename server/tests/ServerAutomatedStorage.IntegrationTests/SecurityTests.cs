using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class SecurityTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        private static readonly object Heartbeat = new { weightGrams = 100.5, rfidOk = true, scaleOk = true };

        [Theory]
        [InlineData("/api/cabinets")]
        [InlineData("/api/items")]
        [InlineData("/api/users")]
        [InlineData("/api/devices")]
        [InlineData("/api/transactions")]
        [InlineData("/api/statistics/dashboard")]
        [InlineData("/api/auth/me")]
        public async Task ProtectedEndpoints_WithoutToken_Return401(string url)
        {
            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Heartbeat_WithoutDeviceKey_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/device/heartbeat", Heartbeat);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Heartbeat_WithWrongDeviceKey_Returns401()
        {
            var response = await SendHeartbeatAsync("wrong-key");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Heartbeat_WithCorrectDeviceKey_Returns204()
        {
            var response = await SendHeartbeatAsync(ApiFactory.DeviceKey);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithoutCookie_Returns401()
        {
            var response = await _client.PostAsync("/api/auth/refresh", content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<HttpResponseMessage> SendHeartbeatAsync(string deviceKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/device/heartbeat")
            {
                Content = JsonContent.Create(Heartbeat)
            };
            request.Headers.Add("X-Device-Key", deviceKey);

            return await _client.SendAsync(request);
        }
    }
}
