using System.Security.Claims;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Infrastructure.Authentication;

namespace ServerAutomatedStorage.WebAPI.Services
{
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public Guid? UserId =>
            Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject), out var id)
                ? id
                : null;

        public Guid RequiredUserId =>
            UserId ?? throw new UnauthorizedException("Пользователь не авторизован.");
    }
}
