using Microsoft.AspNetCore.Authentication.JwtBearer;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Infrastructure.Authentication;

namespace ServerAutomatedStorage.WebAPI.Authorization
{
    public static class AuthorizationSetup
    {
        public const string DevicePolicy = "Device";

        public static string PolicyFor(Permission permission) => $"Permission:{permission}";

        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(DevicePolicy, policy => policy
                    .AddAuthenticationSchemes(DeviceApiKeyDefaults.Scheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim(AuthClaims.Device));

                foreach (var permission in Enum.GetValues<Permission>().Where(p => p != Permission.None))
                {
                    options.AddPolicy(PolicyFor(permission), policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim(AuthClaims.Permission, permission.ToString()));
                }
            });

            return services;
        }
    }
}
