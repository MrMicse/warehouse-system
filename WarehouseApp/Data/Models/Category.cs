namespace WarehouseApp.Data.Models;

public sealed class Category
{
    public long   Id          { get; init; }
    public string Name        { get; init; } = "";
    public string Description { get; init; } = "";
    public string Unit        { get; init; } = "шт.";

    public override string ToString() => Name;
}
