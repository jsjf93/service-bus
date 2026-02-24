using Azure.Messaging.ServiceBus;

namespace ServiceBus.Consumer.Audit;

public class AuditWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AuditWorker> _logger;

    public AuditWorker(ServiceBusClient serviceBusClient, ILogger<AuditWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit worker started");
        // Worker implementation goes here
        await Task.CompletedTask;
    }
}
