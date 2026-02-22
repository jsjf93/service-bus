# Phase 1: Simple Queue Producer/Consumer

This phase is a learning project that sends messages to an Azure Service Bus queue and consumes them with a background worker. The queue name is `tasks` and the message payload is a `TaskCreatedMessage`.

## Prerequisites

- .NET SDK 10 (or later)
- Docker Desktop (for the Azure Service Bus Emulator)
- An Azure Service Bus connection string (real namespace or emulator)

## Quick Start

1. Start the Azure Service Bus Emulator.

   Set the required environment variables and start Docker Compose from the Phase1 folder.

   ```powershell
   # From c:\Git\learning\Azure\service-bus\Phase1
   $env:CONFIG_PATH = "<absolute-path-to>\Phase1\Config.json"
   $env:MSSQL_SA_PASSWORD = "<StrongPasswordHere1!>"
   $env:ACCEPT_EULA = "Y"

   docker compose up -d
   ```

2. Configure the connection string.

   Add your Azure Service Bus connection string to both apps (producer and consumer). You can place it in each `appsettings.json` or store it as a user secret.

   ```json
   "ConnectionStrings": {
     "AzureServiceBus": "<your Azure Service Bus connection string here>"
   }
   ```

3. Run the consumer.

   ```powershell
   cd .\src\ServiceBus.Consumer
   dotnet run
   ```

4. Run the producer in a second terminal.

   ```powershell
   cd .\src\ServiceBus.Producer
   dotnet run
   ```

5. Send messages.

   Type a message in the producer terminal and press Enter. The consumer should log the `TaskCreatedMessage` details.

## Notes

- Queue name: `tasks`
- Message contract: `TaskCreatedMessage` in [src/ServiceBus.Contracts/Messages/TaskCreatedMessage.cs](src/ServiceBus.Contracts/Messages/TaskCreatedMessage.cs)
- Emulator config: [Config.json](Config.json)
- If using the emulator, use the connection string provided by the emulator documentation or its startup output.
