using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Services;

public sealed class OrdersTableService
{
    private readonly TableClient _table;
    public OrdersTableService(TableServiceClient tables) => _table = tables.GetTableClient("Orders");

    public async Task<string> UpdateStatusAsync(string orderDate, string orderId, string requestedStatus, CancellationToken ct = default)
    {
        var target = requestedStatus.Trim();
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Received"] = "Preparing",
            ["Preparing"] = "Ready",
            ["Ready"] = "Collected"
        };
        if (!allowed.Values.Contains(target, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Status may only be Preparing, Ready or Collected.");

        var result = await _table.GetEntityAsync<TableEntity>(orderDate, orderId, cancellationToken: ct);
        var entity = result.Value;
        var currentStatus = entity.TryGetValue("Status", out var statusValue) ? statusValue?.ToString() ?? string.Empty : string.Empty;
        if (!allowed.TryGetValue(currentStatus, out var next) || !next.Equals(target, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid status transition from '{currentStatus}' to '{target}'.");

        entity["Status"] = target;
        entity["LastUpdatedUtc"] = DateTimeOffset.UtcNow;
        await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge, ct);
        return target;
    }
}
