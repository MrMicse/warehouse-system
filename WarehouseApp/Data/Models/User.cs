namespace WarehouseApp.Data.Models;

public sealed class User
{
    public long   Id           { get; init; }
    public string Login        { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string FullName     { get; init; } = "";
    public string Email        { get; init; } = "";
    public string Role         { get; init; } = "storekeeper"; // admin / storekeeper / accountant
    public bool   IsActive     { get; init; } = true;
	public string PasswordSalt { get; set; } = string.Empty;
}
