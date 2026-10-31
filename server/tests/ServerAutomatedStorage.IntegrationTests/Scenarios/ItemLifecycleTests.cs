using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Common.Models;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;

namespace ServerAutomatedStorage.IntegrationTests.Scenarios
{
    [Trait("Category", "Lifecycle")]
    public class ItemLifecycleTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task ItemWithJournal_GoesThroughLowStockArchiveAndBackToCell()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var cell = await admin.NewCellAsync();
            var created = await admin.CreateItemAsync(cell.Id, unitWeight: 20, minQuantity: 5);

            Assert.False(created.IsArchived);
            Assert.Equal(0, created.StockQuantity);
            Assert.True(created.IsLowStock);

            await admin.PostJsonAsync($"/api/items/{created.Id}/replenish", new { quantity = 10 });
            var replenished = await admin.GetItemAsync(created.Id);
            Assert.Equal(10, replenished.StockQuantity);
            Assert.False(replenished.IsLowStock);

            using var device = factory.CreateClient().AsDevice();
            var session = await device.PostJsonAsync("/api/device/sessions",
                ApiClient.Session(ApiClient.Unique("S"), card, cell.Id, before: 200, after: 80));
            var sessionResult = await session.ReadAsync<RecordSessionResponse>();
            Assert.Equal(6, sessionResult.Cells.Single().Quantity);

            var afterWithdrawal = await admin.GetItemAsync(created.Id);
            Assert.Equal(4, afterWithdrawal.StockQuantity);
            Assert.True(afterWithdrawal.IsLowStock);

            var deleted = await admin.DeleteAsync($"/api/items/{created.Id}");
            Assert.True(deleted.IsSuccessStatusCode);
            var archived = await admin.GetItemAsync(created.Id);
            Assert.True(archived.IsArchived);
            Assert.Null(archived.CellId);

            var replenishArchived = await admin.PostJsonAsync($"/api/items/{created.Id}/replenish", new { quantity = 1 });
            Assert.Equal(HttpStatusCode.Conflict, replenishArchived.StatusCode);

            var deleteAgain = await admin.DeleteAsync($"/api/items/{created.Id}");
            Assert.Equal(HttpStatusCode.Conflict, deleteAgain.StatusCode);

            var moved = await admin.PostJsonAsync($"/api/items/{created.Id}/move", new { cellId = cell.Id });
            Assert.True(moved.IsSuccessStatusCode);
            var restored = await admin.GetItemAsync(created.Id);
            Assert.False(restored.IsArchived);
            Assert.Equal(cell.Id, restored.CellId);
            Assert.Equal(4, restored.StockQuantity);
        }

        [Fact]
        public async Task ItemWithoutJournal_IsDeletedPermanently()
        {
            var admin = await factory.AdminClientAsync();
            var cell = await admin.NewCellAsync();
            var item = await admin.CreateItemAsync(cell.Id);

            var deleted = await admin.DeleteAsync($"/api/items/{item.Id}");

            Assert.True(deleted.IsSuccessStatusCode);
            var all = await admin.GetFromJsonAsync<List<Application.Features.Items.DTOs.ItemDTO>>(
                "/api/items?includeArchived=true", ApiClient.Json);
            Assert.DoesNotContain(all!, i => i.Id == item.Id);
        }

        [Fact]
        public async Task ArchivedItem_CanBeMovedOnlyToFreeCell()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 3);
            await admin.DeleteAsync($"/api/items/{item.Id}");
            var occupiedCell = await admin.NewCellAsync();
            await admin.CreateItemAsync(occupiedCell.Id);
            var freeCell = await admin.NewCellAsync();

            var toOccupied = await admin.PostJsonAsync($"/api/items/{item.Id}/move", new { cellId = occupiedCell.Id });
            var toFree = await admin.PostJsonAsync($"/api/items/{item.Id}/move", new { cellId = freeCell.Id });

            Assert.Equal(HttpStatusCode.Conflict, toOccupied.StatusCode);
            Assert.True(toFree.IsSuccessStatusCode);
            Assert.Equal(freeCell.Id, (await admin.GetItemAsync(item.Id)).CellId);
        }

        [Fact]
        public async Task Journal_ContainsEveryStepOfTheScenario()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var cell = await admin.NewCellAsync();
            var item = await admin.CreateItemAsync(cell.Id, unitWeight: 10);
            await admin.PostJsonAsync($"/api/items/{item.Id}/replenish", new { quantity = 5 });
            using var device = factory.CreateClient().AsDevice();
            await device.PostJsonAsync("/api/device/sessions",
                ApiClient.Session(ApiClient.Unique("S"), card, cell.Id, before: 50, after: 30));
            await admin.PostJsonAsync($"/api/items/{item.Id}/adjust", new { actualQuantity = 9 });

            var page = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={item.Id}", ApiClient.Json);

            var types = page!.Items.Select(t => t.Type).OrderBy(t => t).ToList();
            Assert.Equal(["Adjustment", "Replenishment", "Withdrawal"], types);
        }
    }
}
