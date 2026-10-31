using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Application.Features.Roles.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public static class ApiClient
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Unique(string prefix = "") => prefix + Guid.NewGuid().ToString("N")[..8];

        public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(Json);
            return value ?? throw new InvalidOperationException("Empty response body");
        }

        public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object? body = null) =>
            client.PostAsJsonAsync(url, body ?? new { }, Json);

        public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body) =>
            client.PutAsJsonAsync(url, body, Json);

        public static async Task<string> LoginAsync(HttpClient client, string email, string password)
        {
            var response = await client.PostJsonAsync("/api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Login failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await response.ReadAsync<LoginResponse>()).Token;
        }

        public static HttpClient Authorize(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static HttpClient AsDevice(this HttpClient client)
        {
            client.DefaultRequestHeaders.Remove("X-Device-Key");
            client.DefaultRequestHeaders.Add("X-Device-Key", ApiFactory.DeviceKey);
            return client;
        }

        public static async Task<HttpClient> AdminClientAsync(this ApiFactory factory)
        {
            var client = factory.CreateClient();
            return client.Authorize(await factory.AdminTokenAsync());
        }

        public static async Task<Guid> RoleIdAsync(this HttpClient admin, string roleName)
        {
            var roles = await admin.GetFromJsonAsync<List<RoleDTO>>("/api/roles", Json);
            return roles!.Single(r => r.Name == roleName).Id;
        }

        public static async Task<CabinetDTO> CreateCabinetAsync(this HttpClient admin, string? name = null)
        {
            var response = await admin.PostJsonAsync("/api/cabinets", new { name = name ?? Unique("cab-"), location = "Лаборатория" });
            response.EnsureSuccessStatusCode();
            return await response.ReadAsync<CabinetDTO>();
        }

        public static async Task<CellDTO> AddCellAsync(this HttpClient admin, Guid cabinetId, string? name = null)
        {
            var response = await admin.PostJsonAsync($"/api/cabinets/{cabinetId}/cells", new { name = name ?? Unique("cell-") });
            response.EnsureSuccessStatusCode();
            return await response.ReadAsync<CellDTO>();
        }

        public static async Task<CellDTO> NewCellAsync(this HttpClient admin)
        {
            var cabinet = await admin.CreateCabinetAsync();
            return await admin.AddCellAsync(cabinet.Id);
        }

        public static async Task<ItemDTO> CreateItemAsync(
            this HttpClient admin, Guid cellId, double unitWeight = 20, int minQuantity = 0, string? name = null)
        {
            var response = await admin.PostJsonAsync("/api/items",
                new { cellId, name = name ?? Unique("item-"), unitWeightGrams = unitWeight, minQuantity });
            response.EnsureSuccessStatusCode();
            return await response.ReadAsync<ItemDTO>();
        }

        public static async Task<ItemDTO> NewItemAsync(
            this HttpClient admin, int stock = 0, double unitWeight = 20, int minQuantity = 0)
        {
            var cell = await admin.NewCellAsync();
            var item = await admin.CreateItemAsync(cell.Id, unitWeight, minQuantity);
            if (stock > 0)
                (await admin.PostJsonAsync($"/api/items/{item.Id}/replenish", new { quantity = stock })).EnsureSuccessStatusCode();
            return await admin.GetItemAsync(item.Id);
        }

        public static async Task<ItemDTO> GetItemAsync(this HttpClient client, Guid id)
        {
            var items = await client.GetFromJsonAsync<List<ItemDTO>>("/api/items?includeArchived=true", Json);
            return items!.Single(i => i.Id == id);
        }

        public static async Task<UserDTO> CreateUserAsync(
            this HttpClient admin, string roleName, string? cardUid = null, string? email = null, string? password = null)
        {
            var response = await admin.PostJsonAsync("/api/users", new
            {
                fullName = Unique("Сотрудник "),
                roleId = await admin.RoleIdAsync(roleName),
                cardUid,
                email,
                password
            });
            response.EnsureSuccessStatusCode();
            return await response.ReadAsync<UserDTO>();
        }

        public static async Task<(UserDTO User, string Email, string Password, string Token)> CreateSignedInUserAsync(
            this ApiFactory factory, HttpClient admin, string roleName)
        {
            var email = Unique("user") + "@test.local";
            const string password = "Secret123";
            var user = await admin.CreateUserAsync(roleName, null, email, password);
            using var client = factory.CreateClient();
            var token = await LoginAsync(client, email, password);
            return (user, email, password, token);
        }

        public static object Session(string sessionId, string cardUid, Guid cellId, double before, double after, int ageSeconds = 0) =>
            new
            {
                sessionId,
                cardUid,
                ageSeconds,
                cells = new[] { new { cellId, weightBefore = before, weightAfter = after } }
            };
    }
}
