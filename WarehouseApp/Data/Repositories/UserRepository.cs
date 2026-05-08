using MySqlConnector;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Repositories;

public sealed class UserRepository
{
	public async Task<User?> GetByLoginAsync(string login)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            SELECT u.user_id,
                   u.login,
                   u.password_salt,
                   u.password_hash,
                   u.full_name,
                   u.email,
                   r.role_name,
                   u.is_active
            FROM users u
            JOIN roles r ON u.role_id = r.role_id
            WHERE u.login = @login
            LIMIT 1
            """;

		cmd.Parameters.AddWithValue("@login", login);

		await using var reader = await cmd.ExecuteReaderAsync();

		if (!await reader.ReadAsync())
			return null;

		return Map(reader);
	}

	public async Task<List<User>> GetAllAsync()
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            SELECT u.user_id,
                   u.login,
                   u.password_salt,
                   u.password_hash,
                   u.full_name,
                   u.email,
                   r.role_name,
                   u.is_active
            FROM users u
            JOIN roles r ON u.role_id = r.role_id
            ORDER BY u.user_id
            """;

		var list = new List<User>();

		await using var reader = await cmd.ExecuteReaderAsync();

		while (await reader.ReadAsync())
			list.Add(Map(reader));

		return list;
	}

	public async Task<bool> ExistsLoginAsync(string login, long excludeId = 0)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            SELECT COUNT(*)
            FROM users
            WHERE login = @login
              AND user_id <> @id
            """;

		cmd.Parameters.AddWithValue("@login", login);
		cmd.Parameters.AddWithValue("@id", excludeId);

		return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
	}

	public async Task AddAsync(
		string login,
		string passwordSalt,
		string passwordHash,
		string fullName,
		string email,
		string roleName)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            INSERT INTO users
                (login, password_salt, password_hash, full_name, email, role_id, is_active, created_at)
            SELECT
                @login, @salt, @hash, @fname, @email, role_id, 1, NOW()
            FROM roles
            WHERE role_name = @role
            """;

		cmd.Parameters.AddWithValue("@login", login);
		cmd.Parameters.AddWithValue("@salt", passwordSalt);
		cmd.Parameters.AddWithValue("@hash", passwordHash);
		cmd.Parameters.AddWithValue("@fname", fullName);
		cmd.Parameters.AddWithValue("@email", email);
		cmd.Parameters.AddWithValue("@role", roleName);

		await cmd.ExecuteNonQueryAsync();
	}

	public async Task UpdateAsync(
		long id,
		string login,
		string fullName,
		string email,
		string roleName,
		bool isActive)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            UPDATE users
               SET login     = @login,
                   full_name = @fname,
                   email     = @email,
                   role_id   = (SELECT role_id FROM roles WHERE role_name = @role),
                   is_active = @active
             WHERE user_id   = @id
            """;

		cmd.Parameters.AddWithValue("@login", login);
		cmd.Parameters.AddWithValue("@fname", fullName);
		cmd.Parameters.AddWithValue("@email", email);
		cmd.Parameters.AddWithValue("@role", roleName);
		cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
		cmd.Parameters.AddWithValue("@id", id);

		await cmd.ExecuteNonQueryAsync();
	}

	public async Task UpdatePasswordAsync(
		long id,
		string passwordSalt,
		string passwordHash)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
            UPDATE users
               SET password_salt = @salt,
                   password_hash = @hash
             WHERE user_id = @id
            """;

		cmd.Parameters.AddWithValue("@salt", passwordSalt);
		cmd.Parameters.AddWithValue("@hash", passwordHash);
		cmd.Parameters.AddWithValue("@id", id);

		await cmd.ExecuteNonQueryAsync();
	}

	public async Task DeleteAsync(long id)
	{
		await using var conn = Db.CreateConnection();
		await conn.OpenAsync();

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = "DELETE FROM users WHERE user_id = @id";

		cmd.Parameters.AddWithValue("@id", id);

		await cmd.ExecuteNonQueryAsync();
	}

	private static User Map(MySqlDataReader r) => new()
	{
		Id = r.GetInt64("user_id"),
		Login = r.GetString("login"),
		PasswordSalt = r.GetString("password_salt"),
		PasswordHash = r.GetString("password_hash"),
		FullName = r.GetString("full_name"),
		Email = r.GetString("email"),
		Role = r.GetString("role_name"),
		IsActive = r.GetBoolean("is_active"),
	};
}