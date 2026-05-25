using System.Security.Claims;
using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.ServiceDefaults;
using BasicFinance.SharedServiceDefaults;
using ImTools;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
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
    x.UseRuntimeCompilation();
    x.UseFluentValidation();
    x.CodeGeneration.AlwaysUseServiceLocationFor<AppDbContext>();

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
    app.MapScalarApiReference(options =>
        options.AddPreferredSecuritySchemes("Bearer"));
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

await app.RunAsync();

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // Add the security scheme at the document level
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            // Apply it as a requirement for all operations
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}