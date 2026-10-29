using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Gateway.Services;

public sealed class BootstrapUsersService(
    IConfiguration configuration,
    UserService users,
    ILogger<BootstrapUsersService> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await EnsureRoleAsync("Admin", "BootstrapAdmin", ct);
        await EnsureRoleAsync("Staff", "BootstrapStaff", ct);
    }

    private async Task EnsureRoleAsync(string role, string prefix, CancellationToken ct)
    {
        var email = configuration[$"{prefix}:Email"];
        var password = configuration[$"{prefix}:Password"];
        var fullName = configuration[$"{prefix}:FullName"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(fullName))
            return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException($"Configure {prefix}:Email, {prefix}:Password and {prefix}:FullName together, or omit all three.");
        if (password.Length < 12)
            throw new InvalidOperationException($"{prefix}:Password must contain at least 12 characters.");

        if (await users.FindByEmailAsync(email, ct) is not null)
        {
            logger.LogInformation("Bootstrap {Role} account already exists; it was not changed.", role);
            return;
        }

        await users.CreateAsync(role, email, fullName, global::BCrypt.Net.BCrypt.HashPassword(password), ct);
        logger.LogWarning("Created bootstrap {Role} account for configured email {Email}. Remove bootstrap credentials from the runtime environment after verifying the account.", role, UserService.NormalizeEmail(email));
    }
}
