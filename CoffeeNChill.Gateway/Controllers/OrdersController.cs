using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(BackendApiService backend, OrdersTableService orders) : ControllerBase
{
    /// <summary>Submits an order for asynchronous queue processing. This endpoint is public.</summary>
    [AllowAnonymous]
    [HttpPost("queue")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Queue([FromBody] QueueOrderRequest request, CancellationToken ct)
    {
        if (request.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
            return BadRequest(new { error = "SelectedItemSKUs cannot contain blank values." });
        if (request.OrderTimestamp.HasValue && request.OrderTimestamp.Value < DateTimeOffset.UnixEpoch)
            return BadRequest(new { error = "OrderTimestamp is invalid." });
        await backend.ForwardJsonAsync(HttpContext, "api/orders/queue", request, ct);
        return new EmptyResult();
    }

    /// <summary>Moves an order one step forward in its lifecycle. Admin and Staff only.</summary>
    /// <remarks>OrderDate is the yyyyMMdd partition key derived from the order timestamp.</remarks>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        try
        {
            var status = await orders.UpdateStatusAsync(request.OrderDate, request.OrderId, request.Status, ct);
            return Ok(new { orderId = request.OrderId, orderDate = request.OrderDate, status });
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Order was not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
