using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public sealed class QueueOrder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OrderQueueService _queueService;
    private readonly ILogger<QueueOrder> _logger;

    public QueueOrder(OrderQueueService queueService, ILogger<QueueOrder> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    [Function("QueueOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/queue")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        OrderRequest? order;
        try
        {
            order = await JsonSerializer.DeserializeAsync<OrderRequest>(req.Body, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await HttpHelpers.JsonAsync(req, HttpStatusCode.BadRequest, new { error = "Request body must contain valid JSON." });
        }

        var errors = Validate(order);
        if (errors.Count > 0)
            return await HttpHelpers.JsonAsync(req, HttpStatusCode.BadRequest, new { errors });

        var accepted = new OrderRequest
        {
            OrderId = order!.OrderId!.Trim(),
            CustomerName = order.CustomerName!.Trim(),
            SelectedItemSKUs = order.SelectedItemSKUs!.Select(s => s.Trim()).ToList(),
            TotalPrice = order.TotalPrice,
            OrderTimestamp = order.OrderTimestamp?.ToUniversalTime() ?? DateTimeOffset.UtcNow
        };

        try
        {
            await _queueService.EnqueueAsync(accepted, cancellationToken);
            _logger.LogInformation("Order {OrderId} accepted into {QueueName}.", accepted.OrderId, OrderQueueService.QueueName);
            return await HttpHelpers.JsonAsync(req, HttpStatusCode.Accepted, new
            {
                message = "Order accepted for asynchronous processing.",
                orderId = accepted.OrderId,
                queue = OrderQueueService.QueueName,
                orderTimestamp = accepted.OrderTimestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue order {OrderId}.", accepted.OrderId);
            return await HttpHelpers.JsonAsync(req, HttpStatusCode.ServiceUnavailable, new { error = "The order queue is currently unavailable. Please retry." });
        }
    }

    private static List<string> Validate(OrderRequest? order)
    {
        var errors = new List<string>();
        if (order is null) return new List<string> { "A JSON order body is required." };
        if (string.IsNullOrWhiteSpace(order.OrderId)) errors.Add("OrderId is required.");
        else if (order.OrderId.Trim().Length > 64 || !Regex.IsMatch(order.OrderId.Trim(), "^[A-Za-z0-9_-]+$"))
            errors.Add("OrderId must be at most 64 characters and contain only letters, numbers, hyphens or underscores.");
        if (string.IsNullOrWhiteSpace(order.CustomerName)) errors.Add("CustomerName is required.");
        else if (order.CustomerName.Trim().Length > 100) errors.Add("CustomerName must be 100 characters or fewer.");
        if (order.SelectedItemSKUs is null || order.SelectedItemSKUs.Count == 0) errors.Add("SelectedItemSKUs must contain at least one SKU.");
        else if (order.SelectedItemSKUs.Any(s => string.IsNullOrWhiteSpace(s) || s.Trim().Length > 64)) errors.Add("Every selected SKU must be non-empty and at most 64 characters.");
        if (order.TotalPrice <= 0 || order.TotalPrice > 1000000) errors.Add("TotalPrice must be greater than 0 and no more than 1,000,000.");
        if (order.OrderTimestamp.HasValue && order.OrderTimestamp.Value < DateTimeOffset.UnixEpoch) errors.Add("OrderTimestamp is invalid.");
        return errors;
    }
}
