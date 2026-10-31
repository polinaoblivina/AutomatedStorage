using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Common.Models;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class JournalAndStatisticsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private async Task<(HttpClient Admin, Guid ItemId, Guid UserId)> ArrangeJournalAsync()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            var user = await admin.CreateUserAsync("Employee", card);
            var item = await admin.NewItemAsync(stock: 10, unitWeight: 10);
            using var device = factory.CreateClient().AsDevice();
            await device.PostJsonAsync("/api/device/sessions",
                ApiClient.Session(ApiClient.Unique("S"), card, item.CellId!.Value, 100, 70));
            return (admin, item.Id, user.Id);
        }

        [Fact]
        public async Task Journal_ByItem_ReturnsItsOperationsWithNames()
        {
            var (admin, itemId, userId) = await ArrangeJournalAsync();

            var page = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>($"/api/transactions?itemId={itemId}", ApiClient.Json);

            Assert.Equal(2, page!.TotalCount);
            Assert.All(page.Items, t => Assert.Equal(itemId, t.ItemId));
            var withdrawal = page.Items.Single(t => t.Type == "Withdrawal");
            Assert.Equal(userId, withdrawal.UserId);
            Assert.Equal(3, withdrawal.Quantity);
            Assert.False(string.IsNullOrEmpty(withdrawal.ItemName));
            Assert.False(string.IsNullOrEmpty(withdrawal.UserFullName));
            Assert.False(string.IsNullOrEmpty(withdrawal.CabinetName));
        }

        [Theory]
        [InlineData("Withdrawal", 1)]
        [InlineData("Replenishment", 1)]
        [InlineData("Adjustment", 0)]
        public async Task Journal_FilterByType(string type, int expectedForItem)
        {
            var (admin, itemId, _) = await ArrangeJournalAsync();

            var page = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={itemId}&type={type}", ApiClient.Json);

            Assert.Equal(expectedForItem, page!.Items.Count);
            Assert.All(page.Items, t => Assert.Equal(type, t.Type));
        }

        [Fact]
        public async Task Journal_Paging_SplitsResultsAndReportsTotals()
        {
            var (admin, itemId, _) = await ArrangeJournalAsync();

            var first = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={itemId}&pageSize=1&page=1", ApiClient.Json);
            var second = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={itemId}&pageSize=1&page=2", ApiClient.Json);

            Assert.Single(first!.Items);
            Assert.Single(second!.Items);
            Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
            Assert.Equal(2, first.TotalCount);
            Assert.Equal(2, first.TotalPages);
        }

        [Theory]
        [InlineData("page=0")]
        [InlineData("pageSize=0")]
        [InlineData("pageSize=201")]
        [InlineData("type=Unknown")]
        public async Task Journal_WithInvalidParameters_Returns400(string query)
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.GetAsync($"/api/transactions?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Dashboard_ReflectsStockOperationsAndLowStock()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var item = await admin.NewItemAsync(stock: 10, unitWeight: 10, minQuantity: 8);
            using var device = factory.CreateClient().AsDevice();
            await device.PostJsonAsync("/api/device/sessions",
                ApiClient.Session(ApiClient.Unique("S"), card, item.CellId!.Value, 100, 60));

            var dashboard = await admin.GetFromJsonAsync<DashboardDTO>("/api/statistics/dashboard?days=7", ApiClient.Json);

            Assert.True(dashboard!.Totals.Items >= 1);
            Assert.True(dashboard.Totals.WithdrawnToday >= 4);
            Assert.True(dashboard.Totals.OperationsToday >= 2);
            Assert.True(dashboard.Totals.LowStockItems >= 1);
            Assert.Contains(dashboard.LowStock, i => i.Id == item.Id);
            Assert.Contains(dashboard.TopItems, i => i.Id == item.Id);
            Assert.NotEmpty(dashboard.TopUsers);
            Assert.True(dashboard.Daily.Sum(d => d.Withdrawn) >= 4);
        }
    }
}
