using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Infrastructure.Authentication
{
    public sealed class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings = options.Value;

        public AccessToken GenerateToken(User user)
        {
            Claim[] claims =
            [
                new(AuthClaims.Subject, user.Id.ToString()),
                new(AuthClaims.Name, user.FullName),
                new(AuthClaims.Role, user.Role.Name)
            ];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }
    }
}