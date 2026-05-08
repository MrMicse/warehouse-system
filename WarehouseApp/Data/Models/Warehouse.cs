namespace WarehouseApp.Data.Models;

public sealed class Warehouse
{
    public long   Id                  { get; init; }
    public string Name                { get; init; } = "";
    public string Address             { get; init; } = "";
    public long?  ResponsibleUserId   { get; init; }
    public string ResponsibleUserName { get; init; } = "";

    public override string ToString() => Name;
}
