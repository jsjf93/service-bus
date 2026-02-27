using Azure.Messaging.ServiceBus;
using ServiceBus.Contracts;
using ServiceBus.Consumer.Analytics;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true);

builder.Services.AddSingleton(sp => new ServiceBusClient(builder.Configuration.GetConnectionString("AzureServiceBus")));
builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));
builder.Services.AddHostedService<AnalyticsWorker>();

var host = builder.Build();
host.Run();
