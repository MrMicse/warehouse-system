using MySqlConnector;

namespace WarehouseApp.Data;

public static class Db
{
    // ВНИМАНИЕ: для XAMPP по умолчанию пароль root пустой (Pwd=)
    // Если у вас стоит пароль на root - укажите его в Pwd
    public static string ConnectionString =
        "Server=localhost;Port=3306;Database=warehouse_db;Uid=root;Pwd=;SslMode=None;CharSet=utf8mb4;";

    public static MySqlConnection CreateConnection()
        => new MySqlConnection(ConnectionString);
}
