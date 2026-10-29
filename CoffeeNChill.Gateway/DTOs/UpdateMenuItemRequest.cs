using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs;

public sealed class UpdateMenuItemRequest
{
    [Range(0.01, 1000000)]
    public double? Price { get; set; }

    public bool? IsAvailable { get; set; }
}
