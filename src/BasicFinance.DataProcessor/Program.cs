using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.ServiceDefaults;
using BasicFinance.SharedServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register Services
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ServiceDiscoveryNames.BasicFinanceDb)));
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddGoogleServiceAccountCredentials();
builder.Services.AddSingleton<GoogleServiceAccountClient>();

builder.Host.UseWolverine(x =>
{
    x.ListenToRabbitQueue("test-queue");
    x.UseRabbitMqUsingNamedConnection(ServiceDiscoveryNames.RabbitMq)
        .AutoProvision();
});

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

app.Run();