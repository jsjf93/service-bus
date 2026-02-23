using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using ServiceBus.Api.Requests;
using ServiceBus.Api.Responses;
using ServiceBus.Contracts.Messages;

namespace ServiceBus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(
    ServiceBusSender serviceBusSender,
    ILogger<TasksController> logger) : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get() => Ok("Tasks API is running");

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] TaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var taskId = Guid.NewGuid();

        var taskCreatedMessage = new TaskCreatedMessage(taskId, request.Title, request.Description, request.AssignedTo, DateTimeOffset.UtcNow);

        try
        {
            var serviceBusMessage = CreateServiceBusMessage(taskCreatedMessage);
            await serviceBusSender.SendMessageAsync(serviceBusMessage);
            logger.LogInformation("Sent TaskCreatedMessage with TaskId: {TaskId}", taskId);

            return Accepted(new TaskResponse(taskId, taskCreatedMessage.CreatedAt));
        }
        catch (ServiceBusException ex) when (ex.IsTransient)
        {
            logger.LogWarning(ex, "Transient error occurred while sending message to Service Bus. TaskId: {TaskId}", taskId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Error = "Service Unavailable",
                Details = ex.Message
            });
        }
        catch (ServiceBusException ex)
        {
            logger.LogError(ex, "Non-transient error occurred while sending message to Service Bus. TaskId: {TaskId}", taskId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = "Internal Server Error",
                Details = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while processing request. TaskId: {TaskId}", taskId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = "Internal Server Error",
                Details = ex.Message
            });
        }
    }

    private static ServiceBusMessage CreateServiceBusMessage(TaskCreatedMessage taskCreatedMessage)
    {
        var body = BinaryData.FromObjectAsJson(taskCreatedMessage);
        return new ServiceBusMessage(body)
        {
            Subject = TaskCreatedMessage.MessageSubject,
            ContentType = "application/json",
            MessageId = taskCreatedMessage.TaskId.ToString()
        };
    }
}
