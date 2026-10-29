using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs;

public sealed class QueueOrderRequest
{
    [Required, StringLength(64, MinimumLength = 3)]
    public string OrderId { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<string> SelectedItemSKUs { get; set; } = new();

    [Range(0.01, 1000000)]
    public double TotalPrice { get; set; }

    public DateTimeOffset? OrderTimestamp { get; set; }
}
