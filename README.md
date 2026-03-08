# Sample.FirstAgent

A .NET console application demonstrating how to build a conversational AI agent using **Microsoft Agents AI** with **Azure OpenAI** and **Azure Cosmos DB** for persistent chat history.

## Overview

This sample runs as a hosted background service that:

1. Connects to Azure OpenAI and creates a chat agent with tool-calling support.
2. Persists conversation history in Azure Cosmos DB so the agent can recall context across turns.
3. Stores session metadata (user name, channel, company ID) in a state bag attached to the session.
4. Demonstrates a two-turn conversation where the agent remembers information provided in the first turn.

## Features

- **Azure OpenAI integration** via `Azure.AI.OpenAI` and `Microsoft.Agents.AI.OpenAI`
- **Cosmos DB chat history** via `Microsoft.Agents.AI.CosmosNoSql` (`CosmosChatHistoryProvider`)
- **Tool calling** — the agent can call a `GetWeather` function as part of its response
- **Session state bag** for passing arbitrary metadata (user name, channel, company ID) alongside the conversation
- **`AdditionalProperties`** on individual messages for per-message metadata such as the originating channel
- **Session serialization** — the session can be serialized to JSON via `SerializeSessionAsync` for persistence or hand-off

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An **Azure OpenAI** resource with a deployed model (e.g., `gpt-4o-mini`)
- An **Azure Cosmos DB** account (NoSQL API)

## Configuration

Set the following environment variables before running the application:

| Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | The endpoint URL of your Azure OpenAI resource |
| `AZURE_OPENAI_KEY` | API key for your Azure OpenAI resource |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Deployment name (defaults to `gpt-4o-mini`) |
| `AZURE_COSMOS_ENDPOINT` | The endpoint URL of your Azure Cosmos DB account |
| `AZURE_COSMOS_KEY` | API key for your Azure Cosmos DB account |

### Example (PowerShell)

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-openai-resource>.openai.azure.com/"
$env:AZURE_OPENAI_KEY      = "<your-openai-key>"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"
$env:AZURE_COSMOS_ENDPOINT = "https://<your-cosmos-account>.documents.azure.com:443/"
$env:AZURE_COSMOS_KEY      = "<your-cosmos-key>"
```

## Running the Application

```bash
dotnet run
```

On startup the application will:

1. Create (if not already present) a Cosmos DB database named `ChatHistoryDb` with a container named `SessionsContainer` (partition key: `/conversationId`).
2. Start a two-turn conversation with the agent.
3. Log the agent's responses and session metadata to the console.
4. Serialize the session state to JSON and print it to the console.
5. Exit automatically after the conversation completes.

## Project Structure

| File | Description |
|---|---|
| `Program.cs` | Configures the host, registers the `CosmosClient` singleton, and starts the worker |
| `Worker.cs` | Background service that creates the agent, runs the conversation, and logs results |
| `Sample.FirstAgent.csproj` | Project file targeting .NET 10 |

## NuGet Packages

| Package | Version |
|---|---|
| `Azure.AI.OpenAI` | 2.8.0-beta.1 |
| `Microsoft.Agents.AI.CosmosNoSql` | 1.0.0-preview.260304.1 |
| `Microsoft.Agents.AI.OpenAI` | 1.0.0-rc3 |
| `Microsoft.Extensions.Hosting` | 9.0.0 |
