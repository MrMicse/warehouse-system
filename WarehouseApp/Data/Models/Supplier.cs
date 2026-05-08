namespace WarehouseApp.Data.Models;

public sealed class Supplier
{
    public long   Id            { get; init; }
    public string Name          { get; init; } = "";
    public string ContactPerson { get; init; } = "";
    public string Phone         { get; init; } = "";
    public string Email         { get; init; } = "";
    public string Inn           { get; init; } = "";

    public override string ToString() => Name;
}
