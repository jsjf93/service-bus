using System.ComponentModel.DataAnnotations;

namespace ServiceBus.Api.Requests;

public record CreateTaskRequest(
    [Required, MinLength(2)] string Title,
    [Required] string Description,
    [Required] string AssignedTo);
