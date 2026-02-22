using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ServiceBus.Contracts.Messages;

namespace ServiceBus.Consumer;

// Could add ILogger too but just using Console atm
public sealed class TaskConsumerWorker(ServiceBusClient serviceBusClient) : BackgroundService
{
    private ServiceBusProcessor? _serviceBusProcessor;

    private const string QUEUE_NAME = "tasks";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine("[ExecuteAsync] Creating processor...");
        _serviceBusProcessor = serviceBusClient.CreateProcessor(
            QUEUE_NAME, 
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        Console.WriteLine("[ExecuteAsync] Adding handlers...");
        _serviceBusProcessor.ProcessMessageAsync += ProcessMessageHandler;
        _serviceBusProcessor.ProcessErrorAsync += ProcessErrorHandler;

        Console.WriteLine("[ExecuteAsync] Starting processor...");
        await _serviceBusProcessor.StartProcessingAsync(ct);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            // Expected when application shuts down
            Console.WriteLine("[ExecuteAsync] Cancellation requested.");
        }

    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_serviceBusProcessor != null)
        {
            Console.WriteLine("[StopAsync] Stopping processor...");
            await _serviceBusProcessor.StopProcessingAsync(ct);
            await _serviceBusProcessor.DisposeAsync();
            Console.WriteLine("[StopAsync] Stopped processor");
        }

        await base.StopAsync(ct);
    }

    private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            Console.WriteLine($"[ProcessMessageHandler] MessageId: {args.Message.MessageId}");
            Console.WriteLine($"[ProcessMessageHandler] Subject: {args.Message.Subject}");
            Console.WriteLine($"[ProcessMessageHandler] ContentType: {args.Message.ContentType}");
            Console.WriteLine($"[ProcessMessageHandler] EnqueuedTime: {args.Message.EnqueuedTime}");

            var message = JsonSerializer.Deserialize<TaskCreatedMessage>(args.Message.Body)
                ?? throw new JsonException("Unable to deserialise message");

            Console.WriteLine($"[ProcessMessageHandler] Task received. {message}");

            await args.CompleteMessageAsync(args.Message);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ProcessMessageHandler] JsonException thrown: {ex.Message}");
            // Assuming that the message body doesn't match the expected shape so remove it
            await args.DeadLetterMessageAsync(args.Message);
        }
        catch (ServiceBusException ex) when (ex.IsTransient)
        {
            Console.WriteLine($"[ProcessMessageHandler] ServiceBusException thrown: {ex.Message}");
            // Worth retrying as transient error
            await args.AbandonMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessMessageHandler] Exception thrown: {ex.Message}");
            // We don't know so might be worth retrying
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"[ProcessErrorHandler] {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
