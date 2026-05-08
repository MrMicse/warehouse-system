using MySqlConnector;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class SupplierRepository
{
    public async Task<List<Supplier>> GetAllAsync()
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT supplier_id, name, COALESCE(contact_person,'') AS cp,
                   COALESCE(phone,'') AS ph, COALESCE(email,'') AS em,
                   COALESCE(inn,'') AS inn
            FROM suppliers ORDER BY name
            """;
        var list = new List<Supplier>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Supplier
            {
                Id            = reader.GetInt64("supplier_id"),
                Name          = reader.GetString("name"),
                ContactPerson = reader.GetString("cp"),
                Phone         = reader.GetString("ph"),
                Email         = reader.GetString("em"),
                Inn           = reader.GetString("inn"),
            });
        }
        return list;
    }

    public async Task AddAsync(string name, string contactPerson, string phone, string email, string inn)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO suppliers (name, contact_person, phone, email, inn, created_at)
            VALUES (@n, @cp, @ph, @em, @inn, NOW())
            """;
        cmd.Parameters.AddWithValue("@n",   name);
        cmd.Parameters.AddWithValue("@cp",  contactPerson);
        cmd.Parameters.AddWithValue("@ph",  phone);
        cmd.Parameters.AddWithValue("@em",  email);
        cmd.Parameters.AddWithValue("@inn", string.IsNullOrWhiteSpace(inn) ? (object)DBNull.Value : inn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(long id, string name, string contactPerson, string phone, string email, string inn)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE suppliers
               SET name=@n, contact_person=@cp, phone=@ph, email=@em, inn=@inn
             WHERE supplier_id=@id
            """;
        cmd.Parameters.AddWithValue("@n",   name);
        cmd.Parameters.AddWithValue("@cp",  contactPerson);
        cmd.Parameters.AddWithValue("@ph",  phone);
        cmd.Parameters.AddWithValue("@em",  email);
        cmd.Parameters.AddWithValue("@inn", string.IsNullOrWhiteSpace(inn) ? (object)DBNull.Value : inn);
        cmd.Parameters.AddWithValue("@id",  id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM suppliers WHERE supplier_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
