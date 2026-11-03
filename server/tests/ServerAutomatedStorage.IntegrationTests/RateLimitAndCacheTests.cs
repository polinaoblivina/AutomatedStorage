using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class RateLimitAndCacheTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Login_WhenLimitExceeded_Returns429WithRetryAfter()
        {
            var limiter = factory.Services.GetRequiredService<SwitchableRateLimiter>();
            limiter.Deny = true;
            try
            {
                using var client = factory.CreateClient();

                var response = await client.PostJsonAsync("/api/auth/login",
                    new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });

                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.Equal("42", response.Headers.GetValues("Retry-After").Single());
            }
            finally
            {
                limiter.Deny = false;
            }
        }

        [Fact]
        public async Task Dashboard_CalledTwice_ReturnsSameDataFromCache()
        {
            var admin = await factory.AdminClientAsync();

            var first = await admin.GetFromJsonAsync<DashboardDTO>("/api/statistics/dashboard?days=5", ApiClient.Json);
            var second = await admin.GetFromJsonAsync<DashboardDTO>("/api/statistics/dashboard?days=5", ApiClient.Json);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Totals, second.Totals);
            Assert.Equal(first.Daily.Count, second.Daily.Count);
            Assert.Equal(5, second.Daily.Count);
        }
    }
}
