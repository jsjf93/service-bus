namespace ServiceBus.Api.Responses;

public record TaskResponse(
    Guid TaskId, 
    DateTimeOffset CreatedAt, 
    string Message = "Task queued for processing");
