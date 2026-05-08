namespace WarehouseApp.Data.Models;

public sealed class Movement
{
    public long     Id             { get; init; }
    public long     ItemId         { get; init; }
    public string   ItemCode       { get; init; } = "";
    public string   ItemName       { get; init; } = "";
    public string   MovementType   { get; init; } = ""; // 'in' / 'out'
    public string   TypeRu         { get; init; } = ""; // "Приход" / "Расход"
    public int      Quantity       { get; init; }
    public decimal  UnitPrice      { get; init; }
    public decimal  TotalAmount    { get; init; }
    public long?    SupplierId     { get; init; }
    public string   SupplierName   { get; init; } = "";
    public long     WarehouseId    { get; init; }
    public string   WarehouseName  { get; init; } = "";
    public long     CreatedBy      { get; init; }
    public string   CreatedByUser  { get; init; } = "";
    public string   DocumentNumber { get; init; } = "";
    public string   Notes          { get; init; } = "";
    public DateTime MovementDate   { get; init; }
}
