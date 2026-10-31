using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
    public interface IJwtTokenGenerator
    {
        AccessToken GenerateToken(User user);
    }
}
