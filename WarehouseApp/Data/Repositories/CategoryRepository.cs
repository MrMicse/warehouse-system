using MySqlConnector;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class CategoryRepository
{
    public async Task<List<Category>> GetAllAsync()
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT category_id, name, description, unit
            FROM categories ORDER BY name
            """;
        var list = new List<Category>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Category
            {
                Id          = reader.GetInt64("category_id"),
                Name        = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                Unit        = reader.GetString("unit"),
            });
        }
        return list;
    }

    public async Task AddAsync(string name, string description, string unit)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (name, description, unit) VALUES (@n, @d, @u)";
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@d", description);
        cmd.Parameters.AddWithValue("@u", unit);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(long id, string name, string description, string unit)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE categories SET name=@n, description=@d, unit=@u WHERE category_id=@id";
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@d", description);
        cmd.Parameters.AddWithValue("@u", unit);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM categories WHERE category_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
