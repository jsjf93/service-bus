using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;
using ServiceBus.Contracts;
using ServiceBus.Contracts.Messages;
using System.Text.Json;

namespace ServiceBus.Consumer.Email;

public sealed class EmailNotificationWorker(
    ServiceBusClient serviceBusClient, 
    ServiceBusAdministrationClient adminClient,
    IOptions<ServiceBusSettings> settings) : BackgroundService
{
    private readonly string _topicName = settings.Value.TopicName;
    private const string SubscriptionName = "email";
    private const string RuleName = "EmailFilterRule";

    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine("Email notification worker started");

        // An example of configuring a subscription filter to only receive messages that require email notifications.
        // Removed as the Config.json seems to break the emulator when filter rules are added
        //await ConfigureSubscriptionFilterAsync(ct);

        var processor = serviceBusClient.CreateProcessor(
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
            Console.WriteLine("Email notification worker cancellation requested");
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        Console.WriteLine("Email notification worker stopping");
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(ct);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(ct);
    }

    private async Task ConfigureSubscriptionFilterAsync(CancellationToken ct)
    {
        if (await adminClient.RuleExistsAsync(_topicName, SubscriptionName, CreateRuleOptions.DefaultRuleName, ct))
        {
            await adminClient.DeleteRuleAsync(_topicName, SubscriptionName, CreateRuleOptions.DefaultRuleName, ct);
            Console.WriteLine("Default rule removed from subscription");
        }

        if (!await adminClient.RuleExistsAsync(_topicName, SubscriptionName, RuleName, ct))
        {
            var ruleOptions = new CreateRuleOptions(RuleName)
            {
                Filter = new CorrelationRuleFilter
                {
                    ApplicationProperties =
                    {
                        ["RequiresAssignedTo"] = true
                    }
                }
            };
            await adminClient.CreateRuleAsync(_topicName, SubscriptionName, ruleOptions, ct);
            Console.WriteLine("Custom filter rule added to subscription");
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        var subject = message.Subject;
        Console.WriteLine($"Received message with subject: {subject}");

        try
        {
            await Task.Delay(5000);
            var taskCreatedMessage = JsonSerializer.Deserialize<TaskCreatedMessage>(message.Body)
                ?? throw new JsonException("Failed to deserialize message");

            // Simulate email sending
            Console.WriteLine($"Sending email notification for task: {taskCreatedMessage.Title} assigned to {taskCreatedMessage.AssignedTo}");
            Console.WriteLine($"Email notification sent for message with subject: {subject}");
            await args.CompleteMessageAsync(message);
        }
        catch (JsonException ex) {
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
