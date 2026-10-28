namespace CoffeeNChill.Functions.Models;

public sealed class MenuItem
{
    public string Category { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Price { get; set; }
    public bool IsAvailable { get; set; }
}
