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
    public async Task<ActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var taskId = Guid.NewGuid();

        try
        {

            var taskCreatedMessage = new TaskCreatedMessage(
                taskId,
                request.Title,
                request.Description,
                request.AssignedTo,
                DateTimeOffset.UtcNow
            );

            var messageBody = BinaryData.FromObjectAsJson(taskCreatedMessage);

            var message = new ServiceBusMessage(messageBody)
            {
                Subject = TaskCreatedMessage.MessageSubject,
                ContentType = "application/json",
                MessageId = taskCreatedMessage.TaskId.ToString(),
                ApplicationProperties =
                {
                    ["RequiresAssignedTo"] = !string.IsNullOrEmpty(taskCreatedMessage.AssignedTo)
                }
            };

            await serviceBusSender.SendMessageAsync(message);
            logger.LogInformation("Task created with ID: {TaskId}", taskId);
            return Accepted(new CreateTaskResponse(taskId, taskCreatedMessage.CreatedAt));
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
}
