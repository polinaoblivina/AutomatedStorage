using System.Net;
using System.Text;

namespace ServerAutomatedStorage.IntegrationTests.Regression
{
    [Trait("Category", "Regression")]
    public class SecurityRegressionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Theory]
        [Trait("Defect", "D-6")]
        [InlineData(2147483647, HttpStatusCode.BadRequest)]
        [InlineData(100001, HttpStatusCode.BadRequest)]
        [InlineData(100000, HttpStatusCode.OK)]
        public async Task Journal_PageUpperBound_IsValidated(int page, HttpStatusCode expected)
        {
            var admin = await factory.AdminClientAsync();

            var response = await admin.GetAsync($"/api/transactions?page={page}&pageSize=200");

            Assert.Equal(expected, response.StatusCode);
        }

        [Theory]
        [Trait("Defect", "D-7")]
        [InlineData("\"abc\"")]
        [InlineData("99999999999999")]
        [InlineData("")]
        public async Task InvalidJsonBody_ErrorDoesNotRevealInternalTypes(string quantity)
        {
            var body = "{ \"quantity\": " + quantity + (quantity.Length == 0 ? "" : " }");
            var admin = await factory.AdminClientAsync();
            var item = await admin.NewItemAsync();

            var response = await admin.PostAsync($"/api/items/{item.Id}/replenish",
                new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain("ServerAutomatedStorage", text);
            Assert.DoesNotContain("System.", text);
            Assert.Contains("traceId", text);
        }
    }
}
