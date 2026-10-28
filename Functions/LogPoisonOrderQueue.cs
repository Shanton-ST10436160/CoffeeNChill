using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public sealed class LogPoisonOrderQueue
{
    private readonly ILogger<LogPoisonOrderQueue> _logger;

    public LogPoisonOrderQueue(ILogger<LogPoisonOrderQueue> logger) => _logger = logger;

    [Function("LogPoisonOrderQueue")]
    public void Run([QueueTrigger("order-processing-queue-poison", Connection = "AzureWebJobsStorage")] string message)
    {
        string? orderId = null;
        try
        {
            using var document = JsonDocument.Parse(message);
            if (document.RootElement.TryGetProperty("orderId", out var id)) orderId = id.GetString();
            else if (document.RootElement.TryGetProperty("OrderId", out id)) orderId = id.GetString();
        }
        catch (JsonException)
        {
            // The raw message may not be valid JSON; do not throw from the poison logger.
        }

        _logger.LogError("Poison queue message detected. OrderId={OrderId}; Message={Message}", orderId ?? "unknown", message);
    }
}
