using BasicFinance.Infrastructure;
using BasicFinance.MigrationWorker;
using BasicFinance.ServiceDefaults;
using BasicFinance.SharedServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ServiceDiscoveryNames.BasicFinanceDb)));
builder.EnrichNpgsqlDbContext<AppDbContext>();

var host = builder.Build();
await host.RunAsync();