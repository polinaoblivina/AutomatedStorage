using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Infrastructure.Authentication
{
    internal static class AuthenticationSetup
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Jwt section is not configured.");

            if (settings.Secret.Length < 32)
                throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}