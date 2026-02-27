using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using ServiceBus.Api.Responses;
using ServiceBus.Contracts.Messages;
using System.Text.Json;

namespace ServiceBus.Api.Controllers;

public sealed class AdminController(ServiceBusClient serviceBusClient) : ControllerBase
{
    private static readonly string[] Subscriptions = ["analytics", "audit", "email"];

    [HttpGet]
    [Route("api/admin/deadletters")]
    public async Task<IActionResult> GetDeadLetters(CancellationToken ct)
    {
        var deadLetters = await ReceiveDeadLetters(ct);

        return Ok(deadLetters);
    }

    [HttpGet]
    [Route("api/admin/deadletters/{subscription}")]
    public async Task<IActionResult> GetDeadLetterMessages(string subscription, CancellationToken ct)
    {
        if (!Subscriptions.Contains(subscription))
        {
            return BadRequest($"Subscription '{subscription}' does not exist.");
        }

        var messages = await ReceiveDeadLettersByTopicAndSubscription("task-events", subscription, ct);

        var messageDetails = messages.Select(m =>
        {
            TaskCreatedMessage? bodyObject = null;
            string? rawBody = null;

            try
            {
                bodyObject = m.Body.ToObjectFromJson<TaskCreatedMessage>();
            }
            catch (JsonException ex)
            {
                // Deserialization failed - get raw bytes as string
                rawBody = System.Text.Encoding.UTF8.GetString(m.Body);
            }

            return new DeadLetterMessageDetails(
                m.MessageId,
                m.Subject,
                m.EnqueuedTime,
                m.DeadLetterReason,
                m.DeadLetterErrorDescription,
                m.DeliveryCount,
                bodyObject,
                rawBody
            );
        }).ToList();


        return Ok(new DeadLetterInspectionResponse(subscription, messages.Count, messageDetails));
    }

    [HttpPost]
    [Route("api/admin/deadletters/{subscription}/replay")]
    public async Task<IActionResult> ReplayDeadLetters(string subscription, CancellationToken ct)
    {
        if (!Subscriptions.Contains(subscription))
        {
            return BadRequest($"Subscription '{subscription}' does not exist.");
        }

        var result = await ReplayDeadLettersForSubscription("task-events", subscription, ct);

        return Ok(result);
    }

    private async Task<IReadOnlyDictionary<string, DeadLetterCount>> ReceiveDeadLetters(CancellationToken ct)
    {
        var deadLetters = new Dictionary<string, DeadLetterCount>();

        foreach (var subscription in Subscriptions)
        {
            var messages = await ReceiveDeadLettersByTopicAndSubscription("task-events", subscription, ct);
            deadLetters.Add(subscription, new DeadLetterCount(messages.Count));
        }

        return deadLetters;
    }

    private async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveDeadLettersByTopicAndSubscription(string topicName, string subscriptionName, CancellationToken ct)
    {
        await using var receiver = serviceBusClient.CreateReceiver(
            topicName, 
            subscriptionName, 
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

        List<ServiceBusReceivedMessage> messages = new List<ServiceBusReceivedMessage>();

        ServiceBusReceivedMessage? lastMessage = null;

        while (true)
        {
            var batch = await receiver.PeekMessagesAsync(
                maxMessages: 100, 
                fromSequenceNumber: lastMessage?.SequenceNumber + 1, 
                cancellationToken: ct);

            messages.AddRange(batch);

            if (batch.Count != 0)
            {
                lastMessage = batch[^1];
            }

            if (batch.Count < 100)
            {
                break; 
            }
        }

        return messages;
    }

    private async Task<ReplayDeadLettersResponse> ReplayDeadLettersForSubscription(
        string topicName,
        string subscriptionName,
        CancellationToken ct)
    {
        int successful = 0;
        int failed = 0;
        var errors = new List<ReplayError>();

        await using var receiver = serviceBusClient.CreateReceiver(
            topicName,
            subscriptionName,
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

        await using var sender = serviceBusClient.CreateSender(topicName);

        while (true)
        {
            var messages = await receiver.ReceiveMessagesAsync(
                maxMessages: 10,
                maxWaitTime: TimeSpan.FromSeconds(5),
                cancellationToken: ct);

            if (messages.Count == 0)
            {
                break;
            }

            foreach (var message in messages)
            {
                try
                {
                    var newMessage = new ServiceBusMessage(message.Body)
                    {
                        Subject = message.Subject,
                        CorrelationId = message.CorrelationId,
                        MessageId = message.MessageId,
                        ContentType = message.ContentType,
                    };

                    await sender.SendMessageAsync(newMessage, ct);
                    await receiver.CompleteMessageAsync(message, ct);

                    successful++;
                }
                catch (Exception ex)
                {
                    await receiver.AbandonMessageAsync(message, cancellationToken: ct);
                    failed++;
                    errors.Add(new ReplayError(message.MessageId, ex.Message));
                }
            }
        }


        return new ReplayDeadLettersResponse(successful, failed, errors);
    }
}
