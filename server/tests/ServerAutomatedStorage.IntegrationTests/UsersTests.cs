using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class UsersTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private async Task<CardCheckResult> CheckCardAsync(string card)
        {
            using var device = factory.CreateClient().AsDevice();
            var response = await device.PostJsonAsync("/api/device/check-card", new { cardUid = card });
            return (await response.ReadAsync<CheckCardResponse>()).Result;
        }

        [Fact]
        public async Task CreateUser_WithCard_ReturnsUserAndAppearsInList()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");

            var user = await admin.CreateUserAsync("Employee", card);

            Assert.Equal(card.ToUpperInvariant(), user.CardUid);
            Assert.True(user.IsActive);
            Assert.Equal("Employee", user.RoleName);
            var list = await admin.GetFromJsonAsync<List<UserDTO>>("/api/users", ApiClient.Json);
            Assert.Contains(list!, u => u.Id == user.Id);
        }

        [Theory]
        [InlineData("bad-email", "Secret123")]
        [InlineData("valid@test.local", "123")]
        public async Task CreateUser_WithInvalidCredentials_Returns400(string email, string password)
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.PostJsonAsync("/api/users",
                new { fullName = "Тест", roleId = await admin.RoleIdAsync("Employee"), email, password });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AssignCard_MakesCardKnownToDevice()
        {
            var admin = await factory.AdminClientAsync();
            var user = await admin.CreateUserAsync("Employee");
            var card = ApiClient.Unique("CARD");

            var response = await admin.PutJsonAsync($"/api/users/{user.Id}/card", new { cardUid = card });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(CardCheckResult.Granted, await CheckCardAsync(card));
        }

        [Fact]
        public async Task AssignCard_AlreadyUsedByAnotherUser_Returns409()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var other = await admin.CreateUserAsync("Employee");

            var response = await admin.PutJsonAsync($"/api/users/{other.Id}/card", new { cardUid = card });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Theory]
        [InlineData(51)]
        [InlineData(500)]
        public async Task AssignCard_LongerThan50_Returns400(int length)
        {
            var admin = await factory.AdminClientAsync();
            var user = await admin.CreateUserAsync("Employee");

            var response = await admin.PutJsonAsync($"/api/users/{user.Id}/card", new { cardUid = new string('X', length) });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveCard_MakesCardUnknown()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            var user = await admin.CreateUserAsync("Employee", card);

            var response = await admin.DeleteAsync($"/api/users/{user.Id}/card");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(CardCheckResult.UnknownCard, await CheckCardAsync(card));
        }

        [Fact]
        public async Task ChangeRole_ChangesPermissionsImmediately()
        {
            var admin = await factory.AdminClientAsync();
            var (user, _, _, token) = await factory.CreateSignedInUserAsync(admin, "Employee");
            using var client = factory.CreateClient().Authorize(token);
            var before = await client.GetAsync("/api/transactions");

            var change = await admin.PutJsonAsync($"/api/users/{user.Id}/role", new { roleId = await admin.RoleIdAsync("Storekeeper") });
            var after = await client.GetAsync("/api/transactions");

            Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public async Task ChangeRole_OfCurrentUser_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var me = await admin.GetFromJsonAsync<UserDTO>("/api/auth/me", ApiClient.Json);

            var response = await admin.PutJsonAsync($"/api/users/{me!.Id}/role", new { roleId = await admin.RoleIdAsync("Employee") });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateAndActivate_ToggleCardAccess()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            var user = await admin.CreateUserAsync("Employee", card);

            var deactivate = await admin.PostJsonAsync($"/api/users/{user.Id}/deactivate");
            var whileInactive = await CheckCardAsync(card);
            var activate = await admin.PostJsonAsync($"/api/users/{user.Id}/activate");
            var afterActivation = await CheckCardAsync(card);

            Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
            Assert.Equal(CardCheckResult.UserInactive, whileInactive);
            Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
            Assert.Equal(CardCheckResult.Granted, afterActivation);
        }

        [Fact]
        public async Task Deactivate_CurrentUser_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var me = await admin.GetFromJsonAsync<UserDTO>("/api/auth/me", ApiClient.Json);

            var response = await admin.PostJsonAsync($"/api/users/{me!.Id}/deactivate");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("Employee", "/api/users", HttpStatusCode.Forbidden)]
        [InlineData("Employee", "/api/transactions", HttpStatusCode.Forbidden)]
        [InlineData("Employee", "/api/devices", HttpStatusCode.Forbidden)]
        [InlineData("Employee", "/api/items", HttpStatusCode.OK)]
        [InlineData("Storekeeper", "/api/users", HttpStatusCode.Forbidden)]
        [InlineData("Storekeeper", "/api/roles", HttpStatusCode.Forbidden)]
        [InlineData("Storekeeper", "/api/transactions", HttpStatusCode.OK)]
        [InlineData("Storekeeper", "/api/statistics/dashboard", HttpStatusCode.OK)]
        [InlineData("Storekeeper", "/api/devices", HttpStatusCode.OK)]
        [InlineData("Admin", "/api/users", HttpStatusCode.OK)]
        public async Task GetEndpoints_RespectRolePermissions(string role, string url, HttpStatusCode expected)
        {
            var admin = await factory.AdminClientAsync();
            var (_, _, _, token) = await factory.CreateSignedInUserAsync(admin, role);
            using var client = factory.CreateClient().Authorize(token);

            var response = await client.GetAsync(url);

            Assert.Equal(expected, response.StatusCode);
        }

        [Theory]
        [InlineData("Employee", "/api/cabinets")]
        [InlineData("Storekeeper", "/api/cabinets")]
        [InlineData("Employee", "/api/items")]
        public async Task WriteEndpoints_WithoutPermission_Return403(string role, string url)
        {
            var admin = await factory.AdminClientAsync();
            var (_, _, _, token) = await factory.CreateSignedInUserAsync(admin, role);
            using var client = factory.CreateClient().Authorize(token);

            var response = await client.PostJsonAsync(url, new { name = "x", location = "y" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
