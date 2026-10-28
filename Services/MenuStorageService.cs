using Microsoft.Extensions.Configuration;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public sealed class MenuStorageService
{
    private readonly TableClient _table;

    public MenuStorageService(IConfiguration configuration)
    {
        var connection = configuration["StorageConnection"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("StorageConnection is not configured.");

        _table = new TableServiceClient(connection).GetTableClient("MenuItems");
        _table.CreateIfNotExists();
    }

    public async Task CreateAsync(MenuItem item)
    {
        Validate(item);
        var entity = new TableEntity(item.Category, item.Id)
        {
            ["Name"] = item.Name,
            ["Description"] = item.Description,
            ["Price"] = item.Price,
            ["IsAvailable"] = item.IsAvailable
        };
        await _table.AddEntityAsync(entity);
    }

    public async Task<List<MenuItem>> GetAllAsync()
    {
        var result = new List<MenuItem>();
        await foreach (var entity in _table.QueryAsync<TableEntity>())
            result.Add(Map(entity));
        return result;
    }

    public async Task<List<MenuItem>> GetByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.");

        var filter = $"PartitionKey eq '{EscapeOData(category)}'";
        var result = new List<MenuItem>();
        await foreach (var entity in _table.QueryAsync<TableEntity>(filter: filter))
            result.Add(Map(entity));
        return result;
    }

    public async Task UpdateAsync(string category, string id, UpdateMenuItemRequest update)
    {
        if (update.Price is < 0 || (update.Price.HasValue &&
            (double.IsNaN(update.Price.Value) || double.IsInfinity(update.Price.Value))))
            throw new ArgumentException("Price must be a valid non-negative number.");

        if (update.Price is null && update.IsAvailable is null)
            throw new ArgumentException("Provide Price and/or IsAvailable to update.");

        var existing = await _table.GetEntityIfExistsAsync<TableEntity>(category, id);
        if (!existing.HasValue)
            throw new KeyNotFoundException("Menu item not found.");

        var entity = existing.Value ?? throw new KeyNotFoundException("Menu item not found.");
        if (update.Price.HasValue) entity["Price"] = update.Price.Value;
        if (update.IsAvailable.HasValue) entity["IsAvailable"] = update.IsAvailable.Value;

        await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);
    }

    public async Task DeleteAsync(string category, string id)
    {
        var existing = await _table.GetEntityIfExistsAsync<TableEntity>(category, id);
        if (!existing.HasValue)
            throw new KeyNotFoundException("Menu item not found.");

        await _table.DeleteEntityAsync(category, id);
    }

    private static MenuItem Map(TableEntity e) => new()
    {
        Category = e.PartitionKey,
        Id = e.RowKey,
        Name = e.GetString("Name") ?? "",
        Description = e.GetString("Description") ?? "",
        Price = e.GetDouble("Price") ?? 0,
        IsAvailable = e.GetBoolean("IsAvailable") ?? false
    };

    private static string EscapeOData(string value) => value.Replace("'", "''");

    private static void Validate(MenuItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Category) ||
            string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Category, Id and Name are required.");

        if (item.Price < 0 || double.IsNaN(item.Price) || double.IsInfinity(item.Price))
            throw new ArgumentException("Price must be a valid non-negative number.");

        if (item.Category.Length > 100 || item.Id.Length > 100)
            throw new ArgumentException("Category or Id is too long.");
    }
}
