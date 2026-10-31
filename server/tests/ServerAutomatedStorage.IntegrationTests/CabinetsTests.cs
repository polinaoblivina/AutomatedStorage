using System.Net;
using System.Net.Http.Json;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;

namespace ServerAutomatedStorage.IntegrationTests
{
    public class CabinetsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task CreateCabinet_ReturnsCreatedCabinetWithoutCells()
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.PostJsonAsync("/api/cabinets", new { name = "Шкаф 1", location = "Склад" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var cabinet = await response.ReadAsync<CabinetDTO>();
            Assert.Equal("Шкаф 1", cabinet.Name);
            Assert.Equal("Склад", cabinet.Location);
            Assert.Empty(cabinet.Cells);
        }

        [Theory]
        [InlineData(200, 300, HttpStatusCode.Created)]
        [InlineData(201, 300, HttpStatusCode.BadRequest)]
        [InlineData(200, 301, HttpStatusCode.BadRequest)]
        public async Task CreateCabinet_LengthBoundaries(int nameLength, int locationLength, HttpStatusCode expected)
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.PostJsonAsync("/api/cabinets",
                new { name = new string('N', nameLength), location = new string('L', locationLength) });

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task GetCabinet_ReturnsCellsSortedByName()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            await admin.AddCellAsync(cabinet.Id, "B2");
            await admin.AddCellAsync(cabinet.Id, "A1");

            var loaded = await admin.GetFromJsonAsync<CabinetDTO>($"/api/cabinets/{cabinet.Id}", ApiClient.Json);

            Assert.Equal(["A1", "B2"], loaded!.Cells.Select(c => c.Name));
        }

        [Fact]
        public async Task GetCabinet_Unknown_Returns404()
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.GetAsync($"/api/cabinets/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCabinet_ChangesNameAndLocation()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();

            var response = await admin.PutJsonAsync($"/api/cabinets/{cabinet.Id}", new { name = "Новое имя", location = "Новое место" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.ReadAsync<CabinetDTO>();
            Assert.Equal("Новое имя", updated.Name);
            Assert.Equal("Новое место", updated.Location);
        }

        [Fact]
        public async Task AddCell_ReturnsEmptyCell()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();

            var response = await admin.PostJsonAsync($"/api/cabinets/{cabinet.Id}/cells", new { name = "A1" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var cell = await response.ReadAsync<CellDTO>();
            Assert.Equal("A1", cell.Name);
            Assert.Null(cell.Item);
        }

        [Fact]
        public async Task AddCell_WithDuplicateName_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            await admin.AddCellAsync(cabinet.Id, "A1");

            var response = await admin.PostJsonAsync($"/api/cabinets/{cabinet.Id}/cells", new { name = "A1" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RenameCell_ChangesName()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            var cell = await admin.AddCellAsync(cabinet.Id, "A1");

            var response = await admin.PutJsonAsync($"/api/cabinets/{cabinet.Id}/cells/{cell.Id}", new { name = "Z9" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Z9", (await response.ReadAsync<CellDTO>()).Name);
        }

        [Fact]
        public async Task RemoveCell_Empty_Returns204AndCellDisappears()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            var cell = await admin.AddCellAsync(cabinet.Id);

            var response = await admin.DeleteAsync($"/api/cabinets/{cabinet.Id}/cells/{cell.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var loaded = await admin.GetFromJsonAsync<CabinetDTO>($"/api/cabinets/{cabinet.Id}", ApiClient.Json);
            Assert.Empty(loaded!.Cells);
        }

        [Fact]
        public async Task RemoveCell_WithItem_Returns400AndKeepsCell()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            var cell = await admin.AddCellAsync(cabinet.Id);
            await admin.CreateItemAsync(cell.Id);

            var response = await admin.DeleteAsync($"/api/cabinets/{cabinet.Id}/cells/{cell.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var loaded = await admin.GetFromJsonAsync<CabinetDTO>($"/api/cabinets/{cabinet.Id}", ApiClient.Json);
            Assert.Single(loaded!.Cells);
        }

        [Fact]
        public async Task DeleteCabinet_Empty_Returns204ThenNotFound()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            await admin.AddCellAsync(cabinet.Id);

            var delete = await admin.DeleteAsync($"/api/cabinets/{cabinet.Id}");
            var get = await admin.GetAsync($"/api/cabinets/{cabinet.Id}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task DeleteCabinet_WithItems_Returns400AndKeepsCabinet()
        {
            var admin = await factory.AdminClientAsync();
            var cabinet = await admin.CreateCabinetAsync();
            var cell = await admin.AddCellAsync(cabinet.Id);
            await admin.CreateItemAsync(cell.Id);

            var delete = await admin.DeleteAsync($"/api/cabinets/{cabinet.Id}");
            var get = await admin.GetAsync($"/api/cabinets/{cabinet.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }
    }
}
