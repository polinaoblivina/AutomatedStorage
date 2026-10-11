using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace ServerAutomatedStorage.Infrastructure.Authentication
{
    public static class DeviceApiKeyDefaults
    {
        public const string Scheme = "DeviceApiKey";
        public const string HeaderName = "X-Device-Key";
        public const string SectionName = "Devices";
    }

    public sealed class DeviceSettings
    {
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public sealed class DeviceApiKeyOptions : AuthenticationSchemeOptions
    {
        public List<DeviceSettings> Devices { get; set; } = [];
    }

    public sealed class DeviceApiKeyHandler(
        IOptionsMonitor<DeviceApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<DeviceApiKeyOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(DeviceApiKeyDefaults.HeaderName, out var header))
                return Task.FromResult(AuthenticateResult.NoResult());

            var providedKey = Encoding.UTF8.GetBytes(header.ToString());

            var device = Options.Devices.FirstOrDefault(d =>
                !string.IsNullOrEmpty(d.ApiKey) &&
                CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(d.ApiKey), providedKey));

            if (device is null)
                return Task.FromResult(AuthenticateResult.Fail("Invalid device key"));

            Claim[] claims =
            [
                new(AuthClaims.Name, device.Name),
                new(AuthClaims.Device, device.Name)
            ];
            var identity = new ClaimsIdentity(claims, Scheme.Name, AuthClaims.Name, AuthClaims.Role);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
