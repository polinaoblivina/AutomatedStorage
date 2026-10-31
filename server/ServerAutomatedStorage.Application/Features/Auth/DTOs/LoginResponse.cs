using ServerAutomatedStorage.Application.Features.Users.DTOs;

namespace ServerAutomatedStorage.Application.Features.Auth.DTOs
{
    public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDTO User);
}
