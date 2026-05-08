using WarehouseApp.Data.Models;

namespace WarehouseApp.Services;

public static class AppSession
{
    public static User? Current { get; set; }

    public static long   UserId   => Current?.Id   ?? 0;
    public static string UserName => Current?.FullName ?? "";
    public static string RoleName => Current?.Role ?? "";

    public static bool IsAdmin       => RoleName == "admin";
    public static bool IsStorekeeper => RoleName == "storekeeper" || IsAdmin;
    public static bool IsAccountant  => RoleName == "accountant"  || IsAdmin;

    public static void Clear() => Current = null;
}
