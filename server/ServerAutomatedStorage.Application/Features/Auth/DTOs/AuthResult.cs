using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Application.Features.Auth.DTOs
{
    public sealed record AuthResult(LoginResponse Response, NewRefreshToken RefreshToken);
}
