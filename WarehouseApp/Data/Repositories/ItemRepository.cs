using MySqlConnector;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class ItemRepository
{
    /// <summary>
    /// Получает все материалы через VIEW v_items_full (с категорией, складом, статусом запаса).
    /// </summary>
    public async Task<List<Item>> GetAllFromViewAsync(string? filter = null)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        var sql = """
            SELECT item_id, item_code, item_name, category_id, category, unit,
                   unit_price, current_quantity, min_quantity,
                   stock_status, warehouse_id, warehouse
            FROM v_items_full
            """;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            sql += " WHERE item_name LIKE @f OR item_code LIKE @f";
            cmd.Parameters.AddWithValue("@f", $"%{filter}%");
        }
        sql += " ORDER BY item_name";
        cmd.CommandText = sql;

        var list = new List<Item>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Item
            {
                Id              = reader.GetInt64("item_id"),
                ItemCode        = reader.GetString("item_code"),
                Name            = reader.GetString("item_name"),
                CategoryId      = reader.GetInt64("category_id"),
                CategoryName    = reader.GetString("category"),
                Unit            = reader.GetString("unit"),
                UnitPrice       = reader.GetDecimal("unit_price"),
                CurrentQuantity = reader.GetInt32("current_quantity"),
                MinQuantity     = reader.GetInt32("min_quantity"),
                StockStatus     = reader.GetString("stock_status"),
                WarehouseId     = reader.GetInt64("warehouse_id"),
                WarehouseName   = reader.GetString("warehouse"),
            });
        }
        return list;
    }

    /// <summary>
    /// Получает один материал из таблицы items (для редактирования).
    /// </summary>
    public async Task<Item?> GetByIdAsync(long id)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT i.item_id, i.item_code, i.name, COALESCE(i.description,'') AS description,
                   i.category_id, c.name AS category_name, c.unit,
                   i.unit_price, i.min_quantity, i.current_quantity,
                   i.warehouse_id, w.name AS warehouse_name
            FROM items i
            JOIN categories c ON i.category_id = c.category_id
            JOIN warehouses w ON i.warehouse_id = w.warehouse_id
            WHERE i.item_id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Item
        {
            Id              = reader.GetInt64("item_id"),
            ItemCode        = reader.GetString("item_code"),
            Name            = reader.GetString("name"),
            Description     = reader.GetString("description"),
            CategoryId      = reader.GetInt64("category_id"),
            CategoryName    = reader.GetString("category_name"),
            Unit            = reader.GetString("unit"),
            UnitPrice       = reader.GetDecimal("unit_price"),
            MinQuantity     = reader.GetInt32("min_quantity"),
            CurrentQuantity = reader.GetInt32("current_quantity"),
            WarehouseId     = reader.GetInt64("warehouse_id"),
            WarehouseName   = reader.GetString("warehouse_name"),
        };
    }

    public async Task<bool> ExistsCodeAsync(string code, long excludeId = 0)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM items WHERE item_code=@c AND item_id<>@id";
        cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@id", excludeId);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task AddAsync(string code, string name, string description,
        long categoryId, decimal unitPrice, int minQty, long warehouseId)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO items (item_code, name, description, category_id,
                               unit_price, min_quantity, current_quantity,
                               warehouse_id, created_at)
            VALUES (@code, @name, @desc, @cat, @price, @minQ, 0, @wh, NOW())
            """;
        cmd.Parameters.AddWithValue("@code",  code);
        cmd.Parameters.AddWithValue("@name",  name);
        cmd.Parameters.AddWithValue("@desc",  description);
        cmd.Parameters.AddWithValue("@cat",   categoryId);
        cmd.Parameters.AddWithValue("@price", unitPrice);
        cmd.Parameters.AddWithValue("@minQ",  minQty);
        cmd.Parameters.AddWithValue("@wh",    warehouseId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(long id, string code, string name, string description,
        long categoryId, decimal unitPrice, int minQty, long warehouseId)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        // updated_at автоматически обновляется триггером trg_items_updated_at
        cmd.CommandText = """
            UPDATE items
               SET item_code=@code, name=@name, description=@desc,
                   category_id=@cat, unit_price=@price,
                   min_quantity=@minQ, warehouse_id=@wh
             WHERE item_id=@id
            """;
        cmd.Parameters.AddWithValue("@code",  code);
        cmd.Parameters.AddWithValue("@name",  name);
        cmd.Parameters.AddWithValue("@desc",  description);
        cmd.Parameters.AddWithValue("@cat",   categoryId);
        cmd.Parameters.AddWithValue("@price", unitPrice);
        cmd.Parameters.AddWithValue("@minQ",  minQty);
        cmd.Parameters.AddWithValue("@wh",    warehouseId);
        cmd.Parameters.AddWithValue("@id",    id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM items WHERE item_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
