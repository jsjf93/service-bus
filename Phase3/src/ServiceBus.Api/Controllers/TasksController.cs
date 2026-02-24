using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace ServiceBus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(
    ServiceBusSender serviceBusSender,
    ILogger<TasksController> logger) : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get() => Ok("Tasks API is running");
}
