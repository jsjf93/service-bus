using ServiceBus.Contracts.Messages;

namespace ServiceBus.Api.Responses;

public record DeadLetterInspectionResponse(
    string Subscription,
    int TotalCount,
    List<DeadLetterMessageDetails> Messages
);

public record DeadLetterMessageDetails(
    string MessageId,
    string? Subject,
    DateTimeOffset EnqueuedTime,
    string? DeadLetterReason,
    string? DeadLetterErrorDescription,
    int DeliveryCount,
    TaskCreatedMessage? Body,
    string? RawBody
);
