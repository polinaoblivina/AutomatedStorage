using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Infrastructure.Authentication;

namespace ServerAutomatedStorage.WebAPI.Authorization
{
    public static class AuthorizationSetup
    {
        public static string PolicyFor(Permission permission) => $"Permission:{permission}";

        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (var permission in Enum.GetValues<Permission>().Where(p => p != Permission.None))
                {
                    options.AddPolicy(PolicyFor(permission), policy => policy
                        .RequireAuthenticatedUser()
                        .RequireClaim(AuthClaims.Permission, permission.ToString()));
                }
            });

            return services;
        }
    }
}
