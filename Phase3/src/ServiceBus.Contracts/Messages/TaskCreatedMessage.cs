namespace ServiceBus.Contracts.Messages;

public sealed record TaskCreatedMessage(
    Guid TaskId,
    string Title,
    string Description,
    string AssignedTo,
    DateTimeOffset CreatedAt)
{
    public const string MessageSubject = "TaskCreated";
}
