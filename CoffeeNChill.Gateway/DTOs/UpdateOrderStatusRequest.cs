using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs;

public sealed class UpdateOrderStatusRequest
{
    [Required, RegularExpression("^\\d{8}$", ErrorMessage = "OrderDate must be yyyyMMdd.")]
    public string OrderDate { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 3)]
    public string OrderId { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;
}
