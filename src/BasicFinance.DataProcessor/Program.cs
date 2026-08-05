using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.ServiceDefaults;
using BasicFinance.SharedServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ServiceDiscoveryNames.BasicFinanceDb)));
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddGoogleServiceAccountCredentials();
builder.Services.AddSingleton<IGoogleServiceAccountClient, GoogleServiceAccountClient>();

builder.UseWolverine(x =>
{
    x.CodeGeneration.AlwaysUseServiceLocationFor<AppDbContext>();
    x.ListenToRabbitQueue("test-queue");

    x.UseRabbitMqUsingNamedConnection(ServiceDiscoveryNames.RabbitMq)
        .AutoProvision();
    x.UseRuntimeCompilation();
});

var app = builder.Build();
await app.RunAsync();