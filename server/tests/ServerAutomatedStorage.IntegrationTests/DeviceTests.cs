using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Common.Models;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class DeviceTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private async Task<(HttpClient Admin, HttpClient Device, string Card, Guid CellId, Guid ItemId)> ArrangeAsync(
            int stock, double unitWeight = 20)
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var cell = await admin.NewCellAsync();
            var item = await admin.CreateItemAsync(cell.Id, unitWeight);
            if (stock > 0)
                await admin.PostJsonAsync($"/api/items/{item.Id}/replenish", new { quantity = stock });
            return (admin, factory.CreateClient().AsDevice(), card, cell.Id, item.Id);
        }

        private static async Task<RecordSessionResponse> SendSessionAsync(HttpClient device, object session)
        {
            var response = await device.PostJsonAsync("/api/device/sessions", session);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadAsync<RecordSessionResponse>();
        }

        [Fact]
        public async Task CheckCard_UnknownCard_IsDenied()
        {
            using var device = factory.CreateClient().AsDevice();

            var response = await device.PostJsonAsync("/api/device/check-card", new { cardUid = "NO-SUCH-CARD" });

            var result = await response.ReadAsync<CheckCardResponse>();
            Assert.False(result.AccessGranted);
            Assert.Equal(CardCheckResult.UnknownCard, result.Result);
        }

        [Fact]
        public async Task CheckCard_ActiveEmployee_IsGrantedWithUserName()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            var user = await admin.CreateUserAsync("Employee", card);
            using var device = factory.CreateClient().AsDevice();

            var response = await device.PostJsonAsync("/api/device/check-card", new { cardUid = card });

            var result = await response.ReadAsync<CheckCardResponse>();
            Assert.True(result.AccessGranted);
            Assert.Equal(CardCheckResult.Granted, result.Result);
            Assert.Equal(user.FullName, result.UserName);
        }

        [Fact]
        public async Task Session_Withdrawal_ReducesStockAndWritesJournal()
        {
            var (admin, device, card, cellId, itemId) = await ArrangeAsync(stock: 10);

            var result = await SendSessionAsync(device, ApiClient.Session(ApiClient.Unique("S"), card, cellId, 200, 120));

            Assert.True(result.Success);
            var cell = result.Cells.Single();
            Assert.Equal("Withdrawal", cell.TransactionType);
            Assert.Equal(4, cell.Quantity);
            Assert.Equal(6, cell.StockQuantity);
            Assert.Equal(6, (await admin.GetItemAsync(itemId)).StockQuantity);
        }

        [Fact]
        public async Task Session_ReturnedItems_IncreaseStock()
        {
            var (admin, device, card, cellId, itemId) = await ArrangeAsync(stock: 5);

            var result = await SendSessionAsync(device, ApiClient.Session(ApiClient.Unique("S"), card, cellId, 100, 160));

            Assert.Equal("Replenishment", result.Cells.Single().TransactionType);
            Assert.Equal(8, (await admin.GetItemAsync(itemId)).StockQuantity);
        }

        [Theory]
        [InlineData(100, 100)]
        [InlineData(100, 97)]
        [InlineData(100, 103)]
        [InlineData(100, 95.5)]
        public async Task Session_WeightChangeBelowThreshold_RecordsNoOperation(double before, double after)
        {
            var (admin, device, card, cellId, itemId) = await ArrangeAsync(stock: 5);

            var result = await SendSessionAsync(device, ApiClient.Session(ApiClient.Unique("S"), card, cellId, before, after));

            Assert.True(result.Success);
            Assert.Null(result.Cells.Single().TransactionType);
            Assert.Equal(5, (await admin.GetItemAsync(itemId)).StockQuantity);
            var journal = await admin.GetFromJsonAsync<PagedList<TransactionDTO>>(
                $"/api/transactions?itemId={itemId}&type=Withdrawal", ApiClient.Json);
            Assert.Empty(journal!.Items);
        }

        [Fact]
        public async Task Session_SameSessionIdTwice_IsAppliedOnce()
        {
            var (admin, device, card, cellId, itemId) = await ArrangeAsync(stock: 10);
            var session = ApiClient.Session(ApiClient.Unique("S"), card, cellId, 200, 120);

            var first = await SendSessionAsync(device, session);
            var second = await SendSessionAsync(device, session);

            Assert.False(first.AlreadyRecorded);
            Assert.True(second.AlreadyRecorded);
            Assert.Equal(6, (await admin.GetItemAsync(itemId)).StockQuantity);
        }

        [Fact]
        public async Task Session_WithUnknownCard_IsRejectedAndChangesNothing()
        {
            var (admin, device, _, cellId, itemId) = await ArrangeAsync(stock: 10);

            var result = await SendSessionAsync(device, ApiClient.Session(ApiClient.Unique("S"), "NO-SUCH-CARD", cellId, 200, 120));

            Assert.False(result.Success);
            Assert.Equal(10, (await admin.GetItemAsync(itemId)).StockQuantity);
        }

        [Fact]
        public async Task Session_WithSeveralCells_ProcessesEachOne()
        {
            var admin = await factory.AdminClientAsync();
            var card = ApiClient.Unique("CARD");
            await admin.CreateUserAsync("Employee", card);
            var first = await admin.NewItemAsync(stock: 10);
            var second = await admin.NewItemAsync(stock: 10);
            using var device = factory.CreateClient().AsDevice();

            var result = await SendSessionAsync(device, new
            {
                sessionId = ApiClient.Unique("S"),
                cardUid = card,
                ageSeconds = 0,
                cells = new[]
                {
                    new { cellId = first.CellId!.Value, weightBefore = 200.0, weightAfter = 160.0 },
                    new { cellId = second.CellId!.Value, weightBefore = 200.0, weightAfter = 100.0 }
                }
            });

            Assert.Equal(2, result.Cells.Count);
            Assert.Equal(8, (await admin.GetItemAsync(first.Id)).StockQuantity);
            Assert.Equal(5, (await admin.GetItemAsync(second.Id)).StockQuantity);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(604801)]
        public async Task Session_AgeOutOfRange_Returns400(int ageSeconds)
        {
            var (_, device, card, cellId, _) = await ArrangeAsync(stock: 1);

            var response = await device.PostJsonAsync("/api/device/sessions",
                ApiClient.Session(ApiClient.Unique("S"), card, cellId, 10, 5, ageSeconds));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Session_WithSeventeenCells_Returns400()
        {
            var (_, device, card, _, _) = await ArrangeAsync(stock: 1);
            var cells = Enumerable.Range(0, 17)
                .Select(_ => new { cellId = Guid.NewGuid(), weightBefore = 1.0, weightAfter = 1.0 }).ToArray();

            var response = await device.PostJsonAsync("/api/device/sessions",
                new { sessionId = ApiClient.Unique("S"), cardUid = card, ageSeconds = 0, cells });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Heartbeat_MakesDeviceOnlineWithReportedState()
        {
            using var device = factory.CreateClient().AsDevice();
            var admin = await factory.AdminClientAsync();

            var heartbeat = await device.PostJsonAsync("/api/device/heartbeat",
                new { weightGrams = 123.4, rfidOk = true, scaleOk = false, state = "Idle", firmwareVersion = "1.2.3" });
            var statuses = await admin.GetFromJsonAsync<List<DeviceStatusDTO>>("/api/devices", ApiClient.Json);

            Assert.Equal(HttpStatusCode.NoContent, heartbeat.StatusCode);
            var status = statuses!.Single(s => s.Name == ApiFactory.DeviceName);
            Assert.True(status.IsOnline);
            Assert.Equal(123.4, status.WeightGrams);
            Assert.True(status.RfidOk);
            Assert.False(status.ScaleOk);
            Assert.Equal("1.2.3", status.FirmwareVersion);
        }

        [Fact]
        public async Task Events_RecordCardScansAndSessions()
        {
            var (admin, device, card, cellId, _) = await ArrangeAsync(stock: 5);
            await device.PostJsonAsync("/api/device/check-card", new { cardUid = card });
            await SendSessionAsync(device, ApiClient.Session(ApiClient.Unique("S"), card, cellId, 100, 60));

            var scans = await admin.GetFromJsonAsync<List<DeviceEventDTO>>("/api/devices/events?type=CardScan&take=100", ApiClient.Json);
            var sessions = await admin.GetFromJsonAsync<List<DeviceEventDTO>>("/api/devices/events?type=Session&take=100", ApiClient.Json);

            Assert.Contains(scans!, e => string.Equals(e.CardUid, card, StringComparison.OrdinalIgnoreCase) && e.Success);
            Assert.All(scans!, e => Assert.Equal(DeviceEventType.CardScan, e.Type));
            Assert.NotEmpty(sessions!);
        }
    }
}
