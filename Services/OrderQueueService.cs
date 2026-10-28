using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CoffeeNChill.Functions.Services;

public sealed class OrderQueueService
{
    public const string QueueName = "order-processing-queue";
    private readonly QueueClient _queueClient;

    public OrderQueueService(IConfiguration configuration)
    {
        var connectionString = configuration["StorageConnection"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("StorageConnection or AzureWebJobsStorage must be configured.");

        var options = new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };
        _queueClient = new QueueClient(connectionString, QueueName, options);
    }

    public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await _queueClient.SendMessageAsync(json, cancellationToken: cancellationToken);
    }
}
