using Azure.Messaging.ServiceBus;

namespace ServiceBus.Consumer.Analytics;

public class AnalyticsWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AnalyticsWorker> _logger;

    public AnalyticsWorker(ServiceBusClient serviceBusClient, ILogger<AnalyticsWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics worker started");
        // Worker implementation goes here
        await Task.CompletedTask;
    }
}
