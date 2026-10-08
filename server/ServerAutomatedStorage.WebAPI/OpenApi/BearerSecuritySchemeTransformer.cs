using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ServerAutomatedStorage.WebAPI.OpenApi
{
    internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
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

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(Bearer, document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
