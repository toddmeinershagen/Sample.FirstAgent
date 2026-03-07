using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddSingleton(_ => { 
    var endpoint = Environment.GetEnvironmentVariable("AZURE_COSMOS_ENDPOINT")
                ?? throw new InvalidOperationException("Set AZURE_COSMOS_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_COSMOS_KEY")
                ?? throw new InvalidOperationException("Set AZURE_COSMOS_KEY");
    
    var client = new CosmosClient(
    accountEndpoint: endpoint,
    authKeyOrResourceToken: apiKey);
    
    const string databaseId = "ChatHistoryDb";
    const string containerId = "SessionsContainer";

    var databaseResponse = client.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult();
    databaseResponse.Database.CreateContainerIfNotExistsAsync(containerId, "/conversationId").Wait();

    return client;
});

builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();