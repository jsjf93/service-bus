using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBus.Consumer.Email;
using ServiceBus.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true);

var connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");

builder.Services.AddSingleton(sp => new ServiceBusClient(connectionString));
builder.Services.AddSingleton(sp => new ServiceBusAdministrationClient(connectionString));
builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));
builder.Services.AddHostedService<EmailNotificationWorker>();

var host = builder.Build();
host.Run();
