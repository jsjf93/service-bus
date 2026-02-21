namespace ServiceBus.Contracts.Messages;

/// <summary>
/// Message contract for when a new task is created.
/// </summary>
/// <param name="TaskId">Unique identifier for the task.</param>
/// <param name="Title">Title of the task.</param>
/// <param name="Description">Detailed description of the task.</param>
/// <param name="AssignedTo">User to whom the task is assigned.</param>
/// <param name="CreatedAt">Timestamp when the task was created.</param>
public sealed record TaskCreatedMessage(
    Guid TaskId, 
    string Title, 
    string Description,
    string AssignedTo,
    DateTimeOffset CreatedAt);
