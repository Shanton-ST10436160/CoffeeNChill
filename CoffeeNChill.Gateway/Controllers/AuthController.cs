using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserService users, JwtTokenService tokens) : ControllerBase
{
    /// <summary>Registers a customer account with a securely hashed password.</summary>
    /// <response code="201">Customer account created.</response>
    /// <response code="409">Email is already registered.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = UserService.NormalizeEmail(request.Email);
        if (await users.FindByEmailAsync(email, ct) is not null)
            return Conflict(new { error = "An account with this email already exists." });

        try
        {
            await users.CreateAsync("Customer", email, request.FullName, global::BCrypt.Net.BCrypt.HashPassword(request.Password), ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return Conflict(new { error = "An account with this email already exists." });
        }
        return Created("/api/auth/login", new { message = "Customer account created.", email });
    }

    /// <summary>Validates credentials and returns a signed JWT access token.</summary>
    /// <response code="200">Token issued.</response>
    /// <response code="401">Invalid credentials.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email, ct);
        if (user is null)
            return Unauthorized(new { error = "Invalid email or password." });
        if (!global::BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password." });

        var token = tokens.CreateToken(user);
        return Ok(new AuthResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Email = user.RowKey,
            FullName = user.FullName,
            Role = user.PartitionKey
        });
    }
}
