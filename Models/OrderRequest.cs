using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Functions.Models;

public sealed class OrderRequest
{
    [Required]
    public string? OrderId { get; set; }

    [Required]
    public string? CustomerName { get; set; }

    [Required]
    public List<string>? SelectedItemSKUs { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTimeOffset? OrderTimestamp { get; set; }
}
