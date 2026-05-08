using MySqlConnector;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class WarehouseRepository
{
    public async Task<List<Warehouse>> GetAllAsync()
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT w.warehouse_id, w.name, w.address, w.responsible_user_id,
                   COALESCE(u.full_name, '') AS responsible_name
            FROM warehouses w
            LEFT JOIN users u ON w.responsible_user_id = u.user_id
            ORDER BY w.name
            """;
        var list = new List<Warehouse>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Warehouse
            {
                Id                  = reader.GetInt64("warehouse_id"),
                Name                = reader.GetString("name"),
                Address             = reader.IsDBNull(reader.GetOrdinal("address")) ? "" : reader.GetString("address"),
                ResponsibleUserId   = reader.IsDBNull(reader.GetOrdinal("responsible_user_id")) ? null : reader.GetInt64("responsible_user_id"),
                ResponsibleUserName = reader.GetString("responsible_name"),
            });
        }
        return list;
    }

    public async Task AddAsync(string name, string address, long? responsibleUserId)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO warehouses (name, address, responsible_user_id, created_at)
            VALUES (@n, @a, @r, NOW())
            """;
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@a", address);
        cmd.Parameters.AddWithValue("@r", (object?)responsibleUserId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(long id, string name, string address, long? responsibleUserId)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE warehouses
               SET name=@n, address=@a, responsible_user_id=@r
             WHERE warehouse_id=@id
            """;
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@a", address);
        cmd.Parameters.AddWithValue("@r", (object?)responsibleUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM warehouses WHERE warehouse_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
