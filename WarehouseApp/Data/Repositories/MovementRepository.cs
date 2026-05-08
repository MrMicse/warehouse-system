using MySqlConnector;
using System.Data;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class MovementRepository
{
    /// <summary>
    /// Получает все движения с JOIN из 4+ таблиц.
    /// </summary>
    public async Task<List<Movement>> GetAllAsync()
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT m.movement_id, m.movement_date,
                   m.movement_type,
                   CASE m.movement_type
                       WHEN 'in'  THEN 'Приход'
                       WHEN 'out' THEN 'Расход'
                       ELSE 'Перемещение'
                   END AS type_ru,
                   i.item_id, i.item_code, i.name AS item_name,
                   m.quantity, m.unit_price, m.total_amount,
                   COALESCE(s.supplier_id, 0)        AS supplier_id,
                   COALESCE(s.name, '')              AS supplier_name,
                   w.warehouse_id, w.name            AS warehouse_name,
                   u.user_id,      u.full_name       AS created_by_user,
                   COALESCE(m.document_number, '')   AS document_number,
                   COALESCE(m.notes, '')             AS notes
            FROM movements m
            JOIN items      i ON m.item_id      = i.item_id
            JOIN warehouses w ON m.warehouse_id = w.warehouse_id
            JOIN users      u ON m.created_by   = u.user_id
            LEFT JOIN suppliers s ON m.supplier_id = s.supplier_id
            ORDER BY m.movement_date DESC
            """;

        var list = new List<Movement>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Movement
            {
                Id             = reader.GetInt64("movement_id"),
                MovementDate   = reader.GetDateTime("movement_date"),
                MovementType   = reader.GetString("movement_type"),
                TypeRu         = reader.GetString("type_ru"),
                ItemId         = reader.GetInt64("item_id"),
                ItemCode       = reader.GetString("item_code"),
                ItemName       = reader.GetString("item_name"),
                Quantity       = reader.GetInt32("quantity"),
                UnitPrice      = reader.GetDecimal("unit_price"),
                TotalAmount    = reader.GetDecimal("total_amount"),
                SupplierId     = reader.GetInt64("supplier_id"),
                SupplierName   = reader.GetString("supplier_name"),
                WarehouseId    = reader.GetInt64("warehouse_id"),
                WarehouseName  = reader.GetString("warehouse_name"),
                CreatedBy      = reader.GetInt64("user_id"),
                CreatedByUser  = reader.GetString("created_by_user"),
                DocumentNumber = reader.GetString("document_number"),
                Notes          = reader.GetString("notes"),
            });
        }
        return list;
    }

    /// <summary>
    /// Регистрирует приход или расход через хранимую процедуру sp_register_movement.
    /// Процедура сама обновит current_quantity в items и сгенерирует ошибку при недостатке.
    /// </summary>
    public async Task RegisterAsync(long itemId, string type, int quantity,
        decimal unitPrice, long? supplierId, long warehouseId, long userId,
        string documentNumber, string notes)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_register_movement";
        cmd.CommandType  = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("p_item_id",      itemId);
        cmd.Parameters.AddWithValue("p_type",         type);
        cmd.Parameters.AddWithValue("p_quantity",     quantity);
        cmd.Parameters.AddWithValue("p_unit_price",   unitPrice);
        cmd.Parameters.AddWithValue("p_supplier_id",  (object?)supplierId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_warehouse_id", warehouseId);
        cmd.Parameters.AddWithValue("p_user_id",      userId);
        cmd.Parameters.AddWithValue("p_doc_number",   documentNumber);
        cmd.Parameters.AddWithValue("p_notes",        notes);

        await cmd.ExecuteNonQueryAsync();
    }
}
