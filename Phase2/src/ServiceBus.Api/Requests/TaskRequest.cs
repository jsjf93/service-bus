using System.ComponentModel.DataAnnotations;

namespace ServiceBus.Api.Requests;

public record TaskRequest(
    [Required, MinLength(2)] string Title, 
    [Required] string Description, 
    [Required] string AssignedTo);
