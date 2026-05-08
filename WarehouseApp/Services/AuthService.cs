using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;

namespace WarehouseApp.Services;

public sealed class AuthService
{
	private readonly UserRepository _repo = new();

	public async Task<(bool ok, string message, User? user)> LoginAsync(
		string login, string password)
	{
		if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
			return (false, "Введите логин и пароль.", null);

		var user = await _repo.GetByLoginAsync(login);

		if (user is null)
			return (false, "Пользователь не найден.", null);

		if (!user.IsActive)
			return (false, "Учётная запись заблокирована. Обратитесь к администратору.", null);

		if (!PasswordService.Verify(password, user.PasswordSalt, user.PasswordHash))
			return (false, "Неверный пароль.", null);

		return (true, $"Добро пожаловать, {user.FullName}!", user);
	}
}