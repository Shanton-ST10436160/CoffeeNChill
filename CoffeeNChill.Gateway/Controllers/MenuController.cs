using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

[ApiController]
[Route("api/menu")]
public sealed class MenuController(BackendApiService backend) : ControllerBase
{
    /// <summary>Returns all menu items. This endpoint is public.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        await backend.ForwardAsync(HttpContext, "api/menu", ct);
        return new EmptyResult();
    }

    /// <summary>Returns menu items in a category. This endpoint is public.</summary>
    [AllowAnonymous]
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory([FromRoute] string category, CancellationToken ct)
    {
        await backend.ForwardAsync(HttpContext, $"api/menu/category/{Uri.EscapeDataString(category)}", ct);
        return new EmptyResult();
    }

    /// <summary>Creates a menu item. Admin and Staff only.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] MenuItemDto item, CancellationToken ct)
    {
        await backend.ForwardJsonAsync(HttpContext, "api/menu", item, ct);
        return new EmptyResult();
    }

    /// <summary>Updates a menu item. Admin and Staff only.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{category}/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] string category, [FromRoute] string id, [FromBody] UpdateMenuItemRequest item, CancellationToken ct)
    {
        await backend.ForwardJsonAsync(HttpContext, $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", item, ct);
        return new EmptyResult();
    }

    /// <summary>Deletes a menu item. Admin and Staff only.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{category}/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string category, [FromRoute] string id, CancellationToken ct)
    {
        await backend.ForwardAsync(HttpContext, $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", ct);
        return new EmptyResult();
    }
}
