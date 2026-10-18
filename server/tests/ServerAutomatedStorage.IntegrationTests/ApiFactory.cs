using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.IntegrationTests
{
    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public const string DeviceName = "test-cabinet";
        public const string DeviceKey = "test-device-key-0123456789abcdef";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=smartstorage_tests_unused");
            builder.UseSetting("Jwt:Secret", "integration-tests-secret-at-least-32-chars!");
            builder.UseSetting("Jwt:Issuer", "SmartStorage");
            builder.UseSetting("Jwt:Audience", "SmartStorage");
            builder.UseSetting("Devices:0:Name", DeviceName);
            builder.UseSetting("Devices:0:ApiKey", DeviceKey);
        }
    }
}
