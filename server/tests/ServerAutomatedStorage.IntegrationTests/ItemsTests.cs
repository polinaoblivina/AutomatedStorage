using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Common.Models;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class ItemsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task CreateItem_ReturnsItemWithZeroStockInCell()
        {
            var admin = await factory.AdminClientAsync();
            var cell = await admin.NewCellAsync();

            var response = await admin.PostJsonAsync("/api/items",
                new { cellId = cell.Id, name = "Болт М8", unitWeightGrams = 12.5, minQuantity = 3 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var item = await response.ReadAsync<ItemDTO>();
            Assert.Equal("Болт М8", item.Name);
            Assert.Equal(12.5, item.UnitWeightGrams);
            Assert.Equal(0, item.StockQuantity);
            Assert.Equal(3, item.MinQuantity);
            Assert.Equal(cell.Id, item.CellId);
            Assert.False(item.IsArchived);
        }

        [Theory]
        [InlineData("", 10, 0)]
        [InlineData("Имя", 0, 0)]
        [InlineData("Имя", -1, 0)]
        [InlineData("Имя", 10, -1)]
        public async Task CreateItem_WithInvalidData_Returns400(string name, double weight, int minQuantity)
        {
            var admin = await factory.AdminClientAsync();
            var cell = await admin.NewCellAsync();

            var response = await admin.PostJsonAsync("/api/items",
                new { cellId = cell.Id, name, unitWeightGrams = weight, minQuantity });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_InOccupiedCell_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var cell = await admin.NewCellAsync();
            await admin.CreateItemAsync(cell.Id);

            var response = await admin.PostJsonAsync("/api/items",
                new { cellId = cell.Id, name = "Второй", unitWeightGrams = 10, minQuantity = 0 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetItems_ByDefaultHidesArchivedItems()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 2);
            await admin.DeleteAsync($"/api/items/{item.Id}");

            var visible = await admin.GetFromJsonAsync<List<ItemDTO>>("/api/items", ApiClient.Json);
            var all = await admin.GetFromJsonAsync<List<ItemDTO>>("/api/items?includeArchived=true", ApiClient.Json);

            Assert.DoesNotContain(visible!, i => i.Id == item.Id);
            Assert.Contains(all!, i => i.Id == item.Id);
        }

        [Fact]
        public async Task UpdateItem_ChangesNameWeightAndMinimum()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync();

            var response = await admin.PutJsonAsync($"/api/items/{item.Id}",
                new { name = "Переименован", unitWeightGrams = 33.3, minQuantity = 7 });

            Assert.True(response.IsSuccessStatusCode);
            var updated = await admin.GetItemAsync(item.Id);
            Assert.Equal("Переименован", updated.Name);
            Assert.Equal(33.3, updated.UnitWeightGrams);
            Assert.Equal(7, updated.MinQuantity);
        }

        [Theory]
        [InlineData(1, HttpStatusCode.NoContent)]
        [InlineData(100000, HttpStatusCode.NoContent)]
        [InlineData(0, HttpStatusCode.BadRequest)]
        [InlineData(-5, HttpStatusCode.BadRequest)]
        [InlineData(100001, HttpStatusCode.BadRequest)]
        public async Task Replenish_QuantityBoundaries(int quantity, HttpStatusCode expected)
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync();

            var response = await admin.PostJsonAsync($"/api/items/{item.Id}/replenish", new { quantity });

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task Replenish_IncreasesStockAndWritesJournalRecord()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 4);

            await admin.PostJsonAsync($"/api/items/{item.Id}/replenish", new { quantity = 6 });

            Assert.Equal(10, (await admin.GetItemAsync(item.Id)).StockQuantity);
            var journal = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={item.Id}&type=Replenishment", ApiClient.Json);
            Assert.Equal([4, 6], journal!.Items.Select(t => t.Quantity).OrderBy(q => q));
        }

        [Theory]
        [InlineData(0, HttpStatusCode.NoContent)]
        [InlineData(1000000, HttpStatusCode.NoContent)]
        [InlineData(-1, HttpStatusCode.BadRequest)]
        [InlineData(1000001, HttpStatusCode.BadRequest)]
        public async Task Adjust_QuantityBoundaries(int actualQuantity, HttpStatusCode expected)
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 5);

            var response = await admin.PostJsonAsync($"/api/items/{item.Id}/adjust", new { actualQuantity });

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task Adjust_SetsStockAndWritesAdjustmentRecord()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 10);

            await admin.PostJsonAsync($"/api/items/{item.Id}/adjust", new { actualQuantity = 7 });

            Assert.Equal(7, (await admin.GetItemAsync(item.Id)).StockQuantity);
            var journal = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={item.Id}&type=Adjustment", ApiClient.Json);
            Assert.Single(journal!.Items);
        }

        [Fact]
        public async Task Move_ToFreeCell_ChangesCellAndFreesOld()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync(stock: 3);
            var target = await admin.NewCellAsync();

            var response = await admin.PostJsonAsync($"/api/items/{item.Id}/move", new { cellId = target.Id });

            Assert.True(response.IsSuccessStatusCode);
            var moved = await admin.GetItemAsync(item.Id);
            Assert.Equal(target.Id, moved.CellId);
            Assert.Equal(3, moved.StockQuantity);
            var reuse = await admin.PostJsonAsync("/api/items",
                new { cellId = item.CellId, name = "В освободившуюся", unitWeightGrams = 10, minQuantity = 0 });
            Assert.Equal(HttpStatusCode.Created, reuse.StatusCode);
        }

        [Fact]
        public async Task Move_ToOccupiedCell_Returns409AndKeepsPlacement()
        {
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync();
            var other = await admin.NewItemAsync();

            var response = await admin.PostJsonAsync($"/api/items/{item.Id}/move", new { cellId = other.CellId });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(item.CellId, (await admin.GetItemAsync(item.Id)).CellId);
        }

        [Fact]
        public async Task Delete_UnknownItem_Returns404()
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.DeleteAsync($"/api/items/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
