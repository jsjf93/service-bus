namespace ServiceBus.Api.Responses;

public sealed record ReplayDeadLettersResponse(
    int Successful,
    int Failed,
    IReadOnlyList<ReplayError> Errors);

public sealed record ReplayError(
    string MessageId,
    string ErrorDescription);