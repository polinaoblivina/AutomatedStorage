using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using ServerAutomatedStorage.Infrastructure.Authentication;

namespace ServerAutomatedStorage.WebAPI.OpenApi
{
    internal sealed class SecuritySchemesTransformer : IOpenApiDocumentTransformer
    {
        private const string Bearer = "Bearer";

        public Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[Bearer] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Токен из POST /api/auth/login"
            };

            document.Components.SecuritySchemes[DeviceApiKeyDefaults.Scheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = DeviceApiKeyDefaults.HeaderName,
                Description = "Ключ устройства ESP32 (эндпоинты /api/device/*)"
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(Bearer, document)] = []
            });
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(DeviceApiKeyDefaults.Scheme, document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
