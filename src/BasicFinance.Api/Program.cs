using BasicFinance.Domain.Commands;
using BasicFinance.SharedServiceDefaults;
using ImTools;
using Scalar.AspNetCore;
using System.Security.Claims;
using Wolverine;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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
        }
    });


builder.Host.UseWolverine(x =>
{
    x.UseRabbitMqUsingNamedConnection(ServiceDiscoveryNames.RabbitMq)
        .DeclareExchange("test-exchange", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.BindQueue("test-queue", "test-exchangeTotest-queue");
        })
        .AutoProvision();

    x.PublishAllMessages().ToRabbitRoutingKey("test-exchange", "test-exchangeTotest-queue");
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/users/me", async (IMessageBus messageBus, ClaimsPrincipal claimsPrincipal) =>
{
    var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
    await messageBus.PublishAsync(new SyncFinancialData());
    return claims;
})
.RequireAuthorization();

app.Run();
