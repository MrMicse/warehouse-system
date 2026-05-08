using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class MainForm : Form
{
	public MainForm()
	{
		Text = $"Учёт ТМЦ — {AppSession.UserName} [{AppSession.RoleName}]";
		Size = new Size(720, 520);
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;

		// Верхняя панель приветствия
		var lblWelcome = new Label
		{
			Text = $"Добро пожаловать, {AppSession.UserName}!",
			Font = new Font("Segoe UI", 14, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 15),
		};
		var lblRole = new Label
		{
			Text = $"Роль: {RoleRu(AppSession.RoleName)}",
			ForeColor = Color.DimGray,
			AutoSize = true,
			Location = new Point(20, 45),
		};
		Controls.Add(lblWelcome);
		Controls.Add(lblRole);

		// Таблица 2x3 кнопок меню
		var grid = new TableLayoutPanel
		{
			ColumnCount = 3,
			RowCount = 2,
			Location = new Point(20, 90),
			Size = new Size(660, 320),
		};
		for (int i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
		for (int i = 0; i < 2; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

		grid.Controls.Add(MakeBtn("Материалы\n(приход / расход)", () => new ItemsForm().ShowDialog(this)), 0, 0);
		grid.Controls.Add(MakeBtn("Журнал движений", () => new MovementsForm().ShowDialog(this)), 1, 0);
		grid.Controls.Add(MakeBtn("Поставщики", () => new SuppliersForm().ShowDialog(this)), 2, 0);

		var btnCategories = MakeBtn("Категории", () => new CategoriesForm().ShowDialog(this));
		var btnWarehouses = MakeBtn("Склады", () => new WarehousesForm().ShowDialog(this));
		var btnUsers = MakeBtn("Пользователи", () => new UsersForm().ShowDialog(this));

		grid.Controls.Add(btnCategories, 0, 1);
		grid.Controls.Add(btnWarehouses, 1, 1);
		grid.Controls.Add(btnUsers, 2, 1);
		Controls.Add(grid);

		// Управление видимостью по роли
		btnUsers.Enabled = AppSession.IsAdmin;
		btnCategories.Enabled = AppSession.IsAdmin;
		btnWarehouses.Enabled = AppSession.IsAdmin;
		if (!AppSession.IsAdmin)
		{
			btnUsers.Text += "\n(только Admin)";
			btnCategories.Text += "\n(только Admin)";
			btnWarehouses.Text += "\n(только Admin)";
		}

		// Кнопка выхода
		var btnLogout = new Button
		{
			Text = "Выйти",
			Width = 120,
			Height = 32,
			Location = new Point(560, 430),
		};
		btnLogout.Click += (_, _) =>
		{
			AppSession.Clear();
			Close();
		};
		Controls.Add(btnLogout);
	}

	private static Button MakeBtn(string text, Action onClick)
	{
		var b = new Button
		{
			Text = text,
			Dock = DockStyle.Fill,
			Margin = new Padding(8),
			Font = new Font("Segoe UI", 11),
		};
		b.Click += (_, _) => onClick();
		return b;
	}

	private void InitializeComponent()
	{

	}

	private static string RoleRu(string role) => role switch
	{
		"admin" => "Администратор",
		"storekeeper" => "Кладовщик",
		"accountant" => "Бухгалтер",
		_ => role,
	};
}
