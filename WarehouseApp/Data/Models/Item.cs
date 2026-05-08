namespace WarehouseApp.Data.Models;

public sealed class Item
{
    public long    Id              { get; init; }
    public string  ItemCode        { get; init; } = "";
    public string  Name            { get; init; } = "";
    public string  Description     { get; init; } = "";
    public long    CategoryId      { get; init; }
    public string  CategoryName    { get; init; } = "";
    public string  Unit            { get; init; } = "";
    public decimal UnitPrice       { get; init; }
    public int     MinQuantity     { get; init; }
    public int     CurrentQuantity { get; init; }
    public long    WarehouseId     { get; init; }
    public string  WarehouseName   { get; init; } = "";
    public string  StockStatus     { get; init; } = ""; // вычисляется в VIEW

    public override string ToString() => $"{ItemCode} — {Name}";
}
