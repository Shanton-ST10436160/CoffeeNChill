using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs;

public sealed class MenuItemDto
{
    [Required, StringLength(40, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 1)]
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public double Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}
