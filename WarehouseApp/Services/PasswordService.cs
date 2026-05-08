using System.Security.Cryptography;
using System.Text;

namespace WarehouseApp.Services;

public static class PasswordService
{
	public static string GenerateSalt()
	{
		byte[] saltBytes = new byte[16];

		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(saltBytes);

		return BitConverter.ToString(saltBytes)
			.Replace("-", "")
			.ToLowerInvariant();
	}

	public static string HashPassword(string password, string salt)
	{
		using var sha = SHA256.Create();

		byte[] inputBytes = Encoding.UTF8.GetBytes(salt + password);
		byte[] hashBytes = sha.ComputeHash(inputBytes);

		return BitConverter.ToString(hashBytes)
			.Replace("-", "")
			.ToLowerInvariant();
	}

	public static bool Verify(string password, string salt, string storedHash)
	{
		try
		{
			string calculatedHash = HashPassword(password, salt);

			return calculatedHash.Equals(
				storedHash,
				StringComparison.OrdinalIgnoreCase
			);
		}
		catch
		{
			return false;
		}
	}
}