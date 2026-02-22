using Azure.Messaging.ServiceBus;
using ServiceBus.Consumer;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true);

builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureServiceBus")
        ?? throw new InvalidOperationException("Azure Service Bus connection string is not configured.");
    return new ServiceBusClient(connectionString);
});

builder.Services.AddHostedService<TaskConsumerWorker>();

var host = builder.Build();
host.Run();
