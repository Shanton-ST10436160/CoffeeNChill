using CoffeeNChill.Gateway.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeNChill.Gateway.Services;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAtUtc) CreateToken(UserEntity user)
    {
        var key = configuration["Jwt:Key"]!;
        var issuer = configuration["Jwt:Issuer"] ?? "CoffeeNChill";
        var audience = configuration["Jwt:Audience"] ?? "CoffeeNChillClients";
        var expiry = Math.Clamp(configuration.GetValue<int>("Jwt:ExpiryMinutes", 60), 5, 1440);
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(expiry);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.RowKey),
            new(ClaimTypes.Role, user.PartitionKey),
            new(JwtRegisteredClaimNames.Sub, user.RowKey),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, now.UtcDateTime, expires.UtcDateTime, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
