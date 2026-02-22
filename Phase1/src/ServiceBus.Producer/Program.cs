using Azure.Messaging.ServiceBus;﻿
using ServiceBus.Contracts.Messages;
using Microsoft.Extensions.Configuration;

const string QUEUE_NAME = "tasks";

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true);
var config = builder.Build();

var connectionString = config.GetConnectionString("AzureServiceBus")
    ?? throw new InvalidOperationException("Azure Service Bus connection string is not configured.");

// 1. Create ServiceBusClient with connection string
// 2. Create ServiceBusSender for "tasks" queue
// 3. Create your TaskCreatedMessage instance
// 4. Serialize to JSON
// 5. Create ServiceBusMessage with JSON as body
// 6. Send it
// 7. Dispose/close clients

await using var client = new ServiceBusClient(connectionString);
await using var sender = client.CreateSender(QUEUE_NAME);

string? userInput;
bool isRunning = true;

while (isRunning)
{
    Console.Write("Type your message and press Enter to send it or type 'exit' to quit: ");
    userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("Message cannot be empty. Please enter a valid message.");
        continue;
    }
    else if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        isRunning = false;
        Console.WriteLine("Exiting the application. Goodbye!");
        continue;
    }

    await SendTaskMessageAsync(sender, userInput);
}

static async Task SendTaskMessageAsync(ServiceBusSender sender, string title)
{
    var taskMessage = new TaskCreatedMessage(
        TaskId: Guid.NewGuid(),
        Title: title,
        Description: "This task was created to demonstrate sending messages to Azure Service Bus.",
        AssignedTo: "John Doe",
        CreatedAt: DateTimeOffset.UtcNow);

    var messageBody = BinaryData.FromObjectAsJson(taskMessage);
    var serviceBusMessage = new ServiceBusMessage(messageBody)
    {
        MessageId = taskMessage.TaskId.ToString(),
        ContentType = "application/json",
        Subject = "TaskCreated"
    };

    try
    {
        Console.WriteLine($"Sending TaskCreatedMessage with TaskId: {taskMessage.TaskId}...");
        await sender.SendMessageAsync(serviceBusMessage);
        Console.WriteLine($"Sent TaskCreatedMessage with TaskId: {taskMessage.TaskId}");
    }
    catch (ServiceBusException ex) when (ex.IsTransient)
    {
        Console.WriteLine($"Transient error: {ex.Message}. Could retry sending the message.");
    }
    catch (ServiceBusException ex)
    {
        Console.WriteLine($"Error sending message: {ex.Message}. Reason: {ex.Reason}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
}