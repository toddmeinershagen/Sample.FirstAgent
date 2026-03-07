using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ComponentModel;

public class Worker : BackgroundService
{
    private readonly CosmosClient _cosmosClient;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<Worker> _logger;

    public Worker(CosmosClient cosmosClient, IHostApplicationLifetime lifetime, ILogger<Worker> logger)
    {
        _cosmosClient = cosmosClient;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")
                ?? throw new InvalidOperationException("Set AZURE_OPENAI_KEY");

            // var memory1 = new InMemoryChatHistoryProvider();

            const string databaseId = "ChatHistoryDb";
            const string containerId = "SessionsContainer";

            var sessionId = Guid.NewGuid().ToString();
            ChatHistoryProvider memory2 = new CosmosChatHistoryProvider(
                _cosmosClient,
                databaseId,
                containerId,
                _ => new CosmosChatHistoryProvider.State(sessionId));

            var agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new System.ClientModel.ApiKeyCredential(apiKey))
                .GetChatClient(deploymentName)
                .AsAIAgent(new ChatClientAgentOptions()
                {
                    ChatOptions = new()
                    {
                        Instructions = "You are a friendly assistant. Keep your answers brief.",
                        Tools = [AIFunctionFactory.Create(GetWeather)],
                        Temperature = 0.7f,
                        TopP = .95f,
                        Seed = 42,
                    },
                    ChatHistoryProvider = memory2
                });

            AgentSession session = await agent.CreateSessionAsync();
            session.StateBag.SetValue("userName", "Alice");
            session.StateBag.SetValue("channelInitiator", "email");
            session.StateBag.SetValue("companyId", "1234");

            const string channelKey = "channel";

            // First turn
            var message1 = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "My name is Alice and I love hiking.")
            {
                AdditionalProperties = new AdditionalPropertiesDictionary { { channelKey, "email" } }
            };
            _logger.LogInformation("{Response}", await agent.RunAsync(message1, session));

            // Second turn — the agent remembers the user's name and hobby
            var message2 = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "What do you remember about me?")
            {
                AdditionalProperties = new AdditionalPropertiesDictionary { { channelKey, "web" } }
            };
            _logger.LogInformation("{Response}", await agent.RunAsync(message2, session));

            var userName = session.StateBag.GetValue<string>("userName");
            var channelInitiator = session.StateBag.GetValue<string>("channelInitiator");
            var companyId = session.StateBag.GetValue<string>("companyId");

            _logger.LogInformation("The conversation was initiated on the '{ChannelInitiator}' channel by {UserName}. The company is {CompanyId}.", channelInitiator, userName, companyId);

            // var messages = memory1.GetMessages(session);
            // foreach (var message in messages.Where(m => m.Role == ChatRole.User))
            // {
            //     var channel = message.AdditionalProperties?.GetValueOrDefault(channelKey)?.ToString();
            //     var text = message.Contents.OfType<TextContent>().FirstOrDefault()?.Text;
            //     _logger.LogInformation("{Role}: {Text} (channel: {Channel})", message.Role, text, channel);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed with an unexpected error.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    [Description("Get the weather for a given location.")]
    private static string GetWeather([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 15°C.";
}
