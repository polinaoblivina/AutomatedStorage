using Scalar.AspNetCore;
using ServerAutomatedStorage.Application;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Infrastructure;
using ServerAutomatedStorage.Infrastructure.Persistence;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Hubs;
using ServerAutomatedStorage.WebAPI.Middleware;
using ServerAutomatedStorage.WebAPI.OpenApi;
using ServerAutomatedStorage.WebAPI.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddPermissionAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DeviceMonitor>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSignalR()
      .AddJsonProtocol(options =>
          options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<SecuritySchemesTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
    

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await app.Services.InitialiseDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DevicesHub>(DevicesHub.Path, options => options.CloseOnAuthenticationExpiration = true);
app.MapHealthChecks("/health");

app.Run();
