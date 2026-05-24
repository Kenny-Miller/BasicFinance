using System.Security.Claims;
using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.ServiceDefaults;
using BasicFinance.SharedServiceDefaults;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(ServiceDiscoveryNames.Keycloak, realm: "basic-hub", options =>
    {
        options.Audience = "account";
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidIssuers = ["http://localhost:8080/realms/basic-hub"];
        }
    });

// Register Services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ServiceDiscoveryNames.BasicFinanceDb)));
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddGoogleServiceAccountCredentials();
builder.Services.AddSingleton<GoogleServiceAccountClient>();
builder.Services.AddSingleton<GoogleUserClient>();
builder.Services.AddWolverineHttp();

// Configure Wolvering Messaging
builder.UseWolverine(x =>
{
    x.UseFluentValidation();
    x.UseRabbitMqUsingNamedConnection(ServiceDiscoveryNames.RabbitMq)
        .DeclareExchange("test-exchange", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.BindQueue("test-queue", "test-exchangeTotest-queue");
        })
        .AutoProvision();

    x.PublishAllMessages().ToRabbitRoutingKey("test-exchange", "test-exchangeTotest-queue");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Configure Wolvering HTTP
app.MapWolverineEndpoints(opts =>
{
    opts.AddMiddleware(typeof(AuthenticatedUserMiddleware));
    opts.UseFluentValidationProblemDetailMiddleware();
});

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/users/me", async (IMessageBus messageBus, ClaimsPrincipal claimsPrincipal) =>
{
    var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
    return claims;
})
.RequireAuthorization();

app.Run();
