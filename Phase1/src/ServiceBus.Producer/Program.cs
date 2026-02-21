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

Console.Write("Type your message and press Enter to send it: ");
string? userInput;

do
{
    Console.Write("Type your message and press Enter to send it: ");
    userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("Message cannot be empty. Please enter a valid message.");
    }
}
while (string.IsNullOrWhiteSpace(userInput));

await using var client = new ServiceBusClient(connectionString);

var sender = client.CreateSender(QUEUE_NAME);

var taskMessage = new TaskCreatedMessage(
    TaskId: Guid.NewGuid(),
    Title: userInput,
    Description: "This task was created to demonstrate sending messages to Azure Service Bus.",
    AssignedTo: "John Doe",
    CreatedAt: DateTimeOffset.UtcNow);

var messageBody = BinaryData.FromObjectAsJson(taskMessage);

var serviceBusMessage = new ServiceBusMessage(messageBody);

try
{
    Console.WriteLine($"Sending TaskCreatedMessage with TaskId: {taskMessage.TaskId}...");
    await sender.SendMessageAsync(serviceBusMessage);
    Console.WriteLine($"Sent TaskCreatedMessage with TaskId: {taskMessage.TaskId}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error sending message: {ex.Message}");
}

await sender.DisposeAsync();