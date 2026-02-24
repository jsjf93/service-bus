using Azure.Messaging.ServiceBus;

namespace ServiceBus.Consumer.Email;

public class EmailNotificationWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(ServiceBusClient serviceBusClient, ILogger<EmailNotificationWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email notification worker started");
        // Worker implementation goes here
        await Task.CompletedTask;
    }
}
