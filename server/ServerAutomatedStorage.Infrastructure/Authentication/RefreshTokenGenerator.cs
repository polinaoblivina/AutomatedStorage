using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Infrastructure.Authentication
{
    public sealed class RefreshTokenGenerator(IOptions<JwtSettings> options) : IRefreshTokenGenerator
    {
        private readonly JwtSettings _settings = options.Value;

        public NewRefreshToken Generate()
        {
            var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
            return new NewRefreshToken(token, Hash(token), DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
        }

        public string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
