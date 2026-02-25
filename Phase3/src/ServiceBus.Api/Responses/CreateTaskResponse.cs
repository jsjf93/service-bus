namespace ServiceBus.Api.Responses;

public record CreateTaskResponse(
    Guid TaskId,
    DateTimeOffset CreatedAt,
    string Message = "Task queued for processing");
