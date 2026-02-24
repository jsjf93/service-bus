using Azure.Messaging.ServiceBus;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton(sp => new ServiceBusClient(hostContext.Configuration.GetConnectionString("AzureServiceBus")));
});

var host = builder.Build();
host.Run();
