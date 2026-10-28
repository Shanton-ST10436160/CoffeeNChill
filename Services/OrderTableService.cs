using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services;

public sealed class OrderTableService
{
    public const string TableName = "Orders";
    private readonly TableClient _tableClient;

    public OrderTableService(IConfiguration configuration)
    {
        var connectionString = configuration["StorageConnection"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("StorageConnection or AzureWebJobsStorage must be configured.");
        _tableClient = new TableClient(connectionString, TableName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default) =>
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);

    public async Task<OrderEntity?> GetAsync(string partitionKey, string orderId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);
        var result = await _tableClient.GetEntityIfExistsAsync<OrderEntity>(partitionKey, orderId, cancellationToken: cancellationToken);
        return result.HasValue ? result.Value : null;
    }

    public async Task SaveAsync(OrderEntity entity, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }
}
