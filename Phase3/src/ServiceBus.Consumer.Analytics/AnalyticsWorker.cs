using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ServiceBus.Contracts;
using ServiceBus.Contracts.Messages;
using System.Text.Json;

namespace ServiceBus.Consumer.Analytics;

public sealed class AnalyticsWorker(
    ServiceBusClient serviceBusClient, 
    IOptions<ServiceBusSettings> settings) : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
    private readonly string _topicName = settings.Value.TopicName;
    private const string SubscriptionName = "analytics";

    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine("Analytics worker started");

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
        await _processor.StartProcessingAsync(ct);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Analytics worker cancellation requested");
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        Console.WriteLine("Analytics worker stopping");
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
            await Task.Delay(2000);

            // Adding artificial error to show that other consumers aren't impacted
            //throw new Exception("Simulated processing failure");

            var taskCreatedMessage = JsonSerializer.Deserialize<TaskCreatedMessage>(message.Body)
                ?? throw new JsonException("Failed to deserialize message");

            // Simulate analytics processing
            Console.WriteLine($"Recording analytics for task: {taskCreatedMessage.Title} assigned to {taskCreatedMessage.AssignedTo}");
            await Task.Delay(500);
            Console.WriteLine($"Analytics recorded for message with subject: {subject}");
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
