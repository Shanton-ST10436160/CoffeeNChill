using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public sealed class ProcessOrderQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] Lifecycle = ["Received", "Preparing", "Ready", "Collected"];
    private readonly OrderTableService _tableService;
    private readonly ILogger<ProcessOrderQueue> _logger;
    private readonly int _delayMilliseconds;

    public ProcessOrderQueue(OrderTableService tableService, IConfiguration configuration, ILogger<ProcessOrderQueue> logger)
    {
        _tableService = tableService;
        _logger = logger;
        _delayMilliseconds = int.TryParse(configuration["OrderStatusDelayMilliseconds"], out var delay)
            ? Math.Clamp(delay, 0, 10000) : 800;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger(OrderQueueService.QueueName, Connection = "AzureWebJobsStorage")] string message,
        CancellationToken cancellationToken)
    {
        OrderRequest? order;
        try
        {
            order = JsonSerializer.Deserialize<OrderRequest>(message, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Queue message was not valid order JSON. The host will retry it and eventually move it to the poison queue.");
            throw;
        }

        if (order is null || string.IsNullOrWhiteSpace(order.OrderId) || string.IsNullOrWhiteSpace(order.CustomerName) ||
            order.SelectedItemSKUs is null || order.SelectedItemSKUs.Count == 0 || order.TotalPrice <= 0)
        {
            _logger.LogError("Queue message failed order validation. The host will retry it and eventually move it to the poison queue.");
            throw new InvalidDataException("Queue message did not contain a valid order.");
        }

        var orderId = order.OrderId.Trim();
        var orderTimestamp = order.OrderTimestamp?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var partitionKey = orderTimestamp.UtcDateTime.ToString("yyyyMMdd");
        await _tableService.EnsureCreatedAsync(cancellationToken);

        var entity = await _tableService.GetAsync(partitionKey, orderId, cancellationToken);
        if (entity is null)
        {
            entity = new OrderEntity
            {
                PartitionKey = partitionKey,
                RowKey = orderId,
                CustomerName = order.CustomerName.Trim(),
                SelectedItemSKUsJson = JsonSerializer.Serialize(order.SelectedItemSKUs),
                TotalPrice = (double)order.TotalPrice,
                OrderTimestamp = orderTimestamp,
                Status = "Received",
                LastUpdatedUtc = DateTimeOffset.UtcNow
            };
            await _tableService.SaveAsync(entity, cancellationToken);
            _logger.LogInformation("Order {OrderId} recorded with status Received.", orderId);
        }
        else if (entity.Status == "Collected")
        {
            _logger.LogInformation("Duplicate message for already collected order {OrderId}; skipping.", orderId);
            return;
        }

        var currentIndex = Array.IndexOf(Lifecycle, entity.Status);
        if (currentIndex < 0)
        {
            _logger.LogError("Order {OrderId} has unknown status {Status}.", orderId, entity.Status);
            throw new InvalidDataException($"Order {orderId} has an unknown status.");
        }

        for (var i = currentIndex + 1; i < Lifecycle.Length; i++)
        {
            if (_delayMilliseconds > 0) await Task.Delay(_delayMilliseconds, cancellationToken);
            entity.Status = Lifecycle[i];
            entity.LastUpdatedUtc = DateTimeOffset.UtcNow;
            await _tableService.SaveAsync(entity, cancellationToken);
            _logger.LogInformation("Order {OrderId} status changed to {Status}.", orderId, entity.Status);
        }

        _logger.LogInformation("Order {OrderId} processing completed.", orderId);
    }
}
