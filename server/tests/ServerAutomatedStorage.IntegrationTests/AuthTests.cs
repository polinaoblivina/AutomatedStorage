using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private HttpClient NewClient() =>
            factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        private static string RefreshCookie(HttpResponseMessage response) =>
            response.Headers.GetValues("Set-Cookie")
                .First(c => c.StartsWith("refresh_token=", StringComparison.Ordinal))
                .Split(';')[0];

        private static HttpRequestMessage Refresh(string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", cookie);
            return request;
        }

        private async Task<(string Email, string Password)> NewStorekeeperAsync()
        {
            var admin = await factory.AdminClientAsync();
            var (_, email, password, _) = await factory.CreateSignedInUserAsync(admin, "Storekeeper");
            return (email, password);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokenUserAndRefreshCookie()
        {
            var (email, password) = await NewStorekeeperAsync();
            using var client = NewClient();

            var response = await client.PostJsonAsync("/api/auth/login", new { email, password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cookie = response.Headers.GetValues("Set-Cookie").First();
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            var body = await response.ReadAsync<LoginResponse>();
            Assert.False(string.IsNullOrEmpty(body.Token));
            Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
            Assert.Equal(email, body.User.Email);
            Assert.Equal("Storekeeper", body.User.RoleName);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var (email, _) = await NewStorekeeperAsync();
            using var client = NewClient();

            var response = await client.PostJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("", "Secret123")]
        [InlineData("not-an-email", "Secret123")]
        [InlineData("user@test.local", "")]
        public async Task Login_WithInvalidInput_Returns400(string email, string password)
        {
            using var client = NewClient();

            var response = await client.PostJsonAsync("/api/auth/login", new { email, password });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_DeactivatedUser_Returns401()
        {
            var admin = await factory.AdminClientAsync();
            var (user, email, password, _) = await factory.CreateSignedInUserAsync(admin, "Storekeeper");
            await admin.PostJsonAsync($"/api/users/{user.Id}/deactivate");
            using var client = NewClient();

            var response = await client.PostJsonAsync("/api/auth/login", new { email, password });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WithToken_ReturnsCurrentUser()
        {
            var admin = await factory.AdminClientAsync();
            var (user, _, _, token) = await factory.CreateSignedInUserAsync(admin, "Storekeeper");
            using var client = NewClient().Authorize(token);

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var me = await response.ReadAsync<UserDTO>();
            Assert.Equal(user.Id, me.Id);
            Assert.Contains("ViewJournal", me.Permissions);
        }

        [Fact]
        public async Task Refresh_WithValidCookie_ReturnsNewTokenAndNewCookie()
        {
            var (email, password) = await NewStorekeeperAsync();
            using var client = NewClient();
            var login = await client.PostJsonAsync("/api/auth/login", new { email, password });
            var oldCookie = RefreshCookie(login);

            var response = await client.SendAsync(Refresh(oldCookie));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEqual(oldCookie, RefreshCookie(response));
            Assert.False(string.IsNullOrEmpty((await response.ReadAsync<LoginResponse>()).Token));
        }

        [Fact]
        public async Task Refresh_WithAlreadyRotatedCookie_ReturnsConflict()
        {
            var (email, password) = await NewStorekeeperAsync();
            using var client = NewClient();
            var login = await client.PostJsonAsync("/api/auth/login", new { email, password });
            var oldCookie = RefreshCookie(login);
            await client.SendAsync(Refresh(oldCookie));

            var response = await client.SendAsync(Refresh(oldCookie));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Logout_RevokesRefreshToken()
        {
            var (email, password) = await NewStorekeeperAsync();
            using var client = NewClient();
            var login = await client.PostJsonAsync("/api/auth/login", new { email, password });
            var cookie = RefreshCookie(login);
            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            logoutRequest.Headers.Add("Cookie", cookie);

            var logout = await client.SendAsync(logoutRequest);
            var refresh = await client.SendAsync(Refresh(cookie));

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.False(refresh.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithValidData_AllowsLoginWithNewPasswordOnly()
        {
            var admin = await factory.AdminClientAsync();
            var (_, email, password, token) = await factory.CreateSignedInUserAsync(admin, "Storekeeper");
            using var client = NewClient().Authorize(token);

            var change = await client.PostJsonAsync("/api/auth/change-password",
                new { currentPassword = password, newPassword = "NewSecret456" });

            Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
            using var anonymous = NewClient();
            var oldLogin = await anonymous.PostJsonAsync("/api/auth/login", new { email, password });
            var newLogin = await anonymous.PostJsonAsync("/api/auth/login", new { email, password = "NewSecret456" });
            Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
            Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_NewEqualsCurrent_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var (_, _, password, token) = await factory.CreateSignedInUserAsync(admin, "Storekeeper");
            using var client = NewClient().Authorize(token);

            var response = await client.PostJsonAsync("/api/auth/change-password",
                new { currentPassword = password, newPassword = password });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
