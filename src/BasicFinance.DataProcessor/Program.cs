using BasicFinance.DataProcesser;
using BasicFinance.DataProcessor.Handlers;
using BasicFinance.SharedServiceDefaults;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();


builder.Host.UseWolverine(x =>
{
    x.ListenToRabbitQueue("test-queue");
    x.UseRabbitMqUsingNamedConnection(ServiceDiscoveryNames.RabbitMq)
        .AutoProvision();

    Console.WriteLine(x.DescribeHandlerMatch(typeof(SyncFinancialDataHandler)));
});

IConfigurationSection config = builder.Configuration.GetSection("DataProcessor");
builder.Services.Configure<DataProcessorConfig>(config);

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
