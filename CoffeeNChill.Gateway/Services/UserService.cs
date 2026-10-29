using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services;

public sealed class UserService
{
    private static readonly string[] Roles = ["Customer", "Staff", "Admin"];
    private readonly TableClient _table;

    public UserService(TableServiceClient tables) => _table = tables.GetTableClient("Users");

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public Task EnsureTableAsync(CancellationToken ct = default) =>
        _table.CreateIfNotExistsAsync(ct);

    public async Task<UserEntity?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await _table.CreateIfNotExistsAsync(ct);
        var key = NormalizeEmail(email);
        foreach (var role in Roles)
        {
            try
            {
                var result = await _table.GetEntityAsync<UserEntity>(role, key, cancellationToken: ct);
                return result.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Not in this role partition; check the next one.
            }
        }
        return null;
    }

    public async Task CreateAsync(string role, string email, string fullName, string passwordHash, CancellationToken ct = default)
    {
        if (!Roles.Contains(role, StringComparer.Ordinal))
            throw new ArgumentException("Unsupported role.", nameof(role));

        await _table.CreateIfNotExistsAsync(ct);
        var entity = new UserEntity
        {
            PartitionKey = role,
            RowKey = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            CreatedDate = DateTimeOffset.UtcNow
        };
        await _table.AddEntityAsync(entity, ct);
    }

    public async Task CreateIfMissingAsync(string email, string password, string fullName, string role, CancellationToken ct = default)
    {
        if (await FindByEmailAsync(email, ct) is not null)
            return;

        await CreateAsync(role, email, fullName, global::BCrypt.Net.BCrypt.HashPassword(password), ct);
    }
}
