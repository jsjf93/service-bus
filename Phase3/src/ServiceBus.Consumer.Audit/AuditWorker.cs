using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ServiceBus.Contracts;
using ServiceBus.Contracts.Messages;
using System.Text.Json;

namespace ServiceBus.Consumer.Audit;

public sealed class AuditWorker(
    ServiceBusClient serviceBusClient, 
    IOptions<ServiceBusSettings> settings) : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
    private readonly string _topicName = settings.Value.TopicName;
    private const string SubscriptionName = "audit";

    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine("Audit worker started");

        var processor = _serviceBusClient.CreateProcessor(
            _topicName,
            SubscriptionName,
            new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        _processor = processor;
        await _processor.StartProcessingAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Audit worker cancellation requested");
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        Console.WriteLine("Audit worker stopping");
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(ct);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(ct);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        var subject = message.Subject;
        Console.WriteLine($"Received message with subject: {subject}");

        try
        {
            await Task.Delay(3000); // Simulate processing time.
            var taskCreatedMessage = JsonSerializer.Deserialize<TaskCreatedMessage>(message.Body)
                ?? throw new JsonException("Failed to deserialize message");

            // Simulate audit logging
            Console.WriteLine($"Recording audit entry for task: {taskCreatedMessage.Title} assigned to {taskCreatedMessage.AssignedTo}");
            Console.WriteLine($"Audit entry recorded for message with subject: {subject}");
            await args.CompleteMessageAsync(message);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to process message with subject: {subject}. Error: {ex.Message}");
            await args.DeadLetterMessageAsync(message);
        }
        catch (ServiceBusException ex)
        {
            Console.WriteLine($"ServiceBusException occurred while processing message with subject: {subject}. Error: {ex.Message}");
            await args.AbandonMessageAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error occurred while processing message with subject: {subject}. Error: {ex.Message}");
            await args.AbandonMessageAsync(message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Error in message processing: {args.ErrorSource}. Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
