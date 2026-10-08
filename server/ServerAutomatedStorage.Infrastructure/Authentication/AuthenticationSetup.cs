using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using System.Security.Claims;
using System.Text;

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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = settings.Issuer,
                            ValidAudience = settings.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                            NameClaimType = AuthClaims.Name,
                            RoleClaimType = AuthClaims.Role,
                            ClockSkew = TimeSpan.FromMinutes(1)
                        };
                        options.Events = new JwtBearerEvents { OnTokenValidated = RefreshPrincipalAsync };
                    });

            return services;
        }

        private static async Task RefreshPrincipalAsync(TokenValidatedContext context)
        {
            var subject = context.Principal?.FindFirstValue(AuthClaims.Subject);
            if (!Guid.TryParse(subject, out var userId))
            {
                context.Fail("Invalid token subject");
                return;
            }

            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = await users.GetByIdWithRoleAsync(userId, context.HttpContext.RequestAborted);
            if (user is null || !user.IsActive)
            {
                context.Fail("User was not found or deactivated");
                return;
            }

            var claims = new List<Claim>
            {
                new(AuthClaims.Subject, user.Id.ToString()),
                new(AuthClaims.Name, user.FullName),
                new(AuthClaims.Role, user.Role.Name)
            };
            claims.AddRange(Enum.GetValues<Permission>()
                .Where(p => p != Permission.None && user.Role.Has(p))
                .Select(p => new Claim(AuthClaims.Permission, p.ToString())));

            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                claims, JwtBearerDefaults.AuthenticationScheme, AuthClaims.Name, AuthClaims.Role));
        }
    }
}