using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class UsersForm : Form
{
	private readonly UserRepository _repo = new();

	private readonly DataGridView _grid = new()
	{
		Dock = DockStyle.Fill,
		ReadOnly = true,
		SelectionMode = DataGridViewSelectionMode.FullRowSelect,
		MultiSelect = false,
		AllowUserToAddRows = false,
	};

	private readonly Button _btnAdd = new() { Text = "Добавить", Width = 110 };
	private readonly Button _btnEdit = new() { Text = "Изменить", Width = 110 };
	private readonly Button _btnPwd = new() { Text = "Сменить пароль", Width = 130 };
	private readonly Button _btnDelete = new() { Text = "Удалить", Width = 100 };
	private readonly Button _btnRefresh = new() { Text = "🔄", Width = 40 };

	public UsersForm()
	{
		Text = "Пользователи системы";
		Size = new Size(820, 460);
		StartPosition = FormStartPosition.CenterParent;

		_grid.AutoGenerateColumns = false;

		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = "Id",
			HeaderText = "ID",
			Width = 50,
			Visible = false
		});

		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = "Login",
			HeaderText = "Логин",
			Width = 130
		});

		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = "FullName",
			HeaderText = "ФИО",
			Width = 220
		});

		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = "Email",
			HeaderText = "Email",
			Width = 180
		});

		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			DataPropertyName = "Role",
			HeaderText = "Роль",
			Width = 120
		});

		_grid.Columns.Add(new DataGridViewCheckBoxColumn
		{
			DataPropertyName = "IsActive",
			HeaderText = "Активен",
			Width = 70
		});

		var btnPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Bottom,
			Height = 45,
			Padding = new Padding(5)
		};

		btnPanel.Controls.AddRange(new Control[]
		{
			_btnAdd,
			_btnEdit,
			_btnPwd,
			_btnDelete,
			_btnRefresh
		});

		Controls.Add(_grid);
		Controls.Add(btnPanel);

		_btnAdd.Click += async (_, _) => await OnAddClick();
		_btnEdit.Click += async (_, _) => await OnEditClick();
		_btnPwd.Click += async (_, _) => await OnPwdClick();
		_btnDelete.Click += async (_, _) => await OnDeleteClick();
		_btnRefresh.Click += async (_, _) => await LoadAsync();

		Load += async (_, _) => await LoadAsync();
	}

	private async Task LoadAsync()
	{
		try
		{
			_grid.DataSource = await _repo.GetAllAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Ошибка: {ex.Message}",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}
	}

	private User? GetSelected()
	{
		return _grid.SelectedRows.Count == 0
			? null
			: _grid.SelectedRows[0].DataBoundItem as User;
	}

	private async Task OnAddClick()
	{
		using var dlg = new UserEditDialog(null);

		if (dlg.ShowDialog(this) != DialogResult.OK)
			return;

		try
		{
			if (await _repo.ExistsLoginAsync(dlg.LoginValue))
			{
				MessageBox.Show(
					"Пользователь с таким логином уже существует.",
					"Ошибка",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning
				);
				return;
			}

			string salt = PasswordService.GenerateSalt();
			string hash = PasswordService.HashPassword(dlg.PasswordValue, salt);

			await _repo.AddAsync(
				dlg.LoginValue,
				salt,
				hash,
				dlg.FullNameValue,
				dlg.EmailValue,
				dlg.RoleValue
			);

			await LoadAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Ошибка: {ex.Message}",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}
	}

	private async Task OnEditClick()
	{
		var sel = GetSelected();

		if (sel is null)
		{
			MessageBox.Show("Выберите пользователя.");
			return;
		}

		using var dlg = new UserEditDialog(sel);

		if (dlg.ShowDialog(this) != DialogResult.OK)
			return;

		try
		{
			if (await _repo.ExistsLoginAsync(dlg.LoginValue, sel.Id))
			{
				MessageBox.Show(
					"Пользователь с таким логином уже существует.",
					"Ошибка",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning
				);
				return;
			}

			await _repo.UpdateAsync(
				sel.Id,
				dlg.LoginValue,
				dlg.FullNameValue,
				dlg.EmailValue,
				dlg.RoleValue,
				dlg.IsActiveValue
			);

			await LoadAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Ошибка: {ex.Message}",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}
	}

	private async Task OnPwdClick()
	{
		var sel = GetSelected();

		if (sel is null)
		{
			MessageBox.Show("Выберите пользователя.");
			return;
		}

		using var dlg = new PasswordChangeDialog();

		if (dlg.ShowDialog(this) != DialogResult.OK ||
			string.IsNullOrWhiteSpace(dlg.NewPassword))
		{
			return;
		}

		try
		{
			string salt = PasswordService.GenerateSalt();
			string hash = PasswordService.HashPassword(dlg.NewPassword, salt);

			await _repo.UpdatePasswordAsync(
				sel.Id,
				salt,
				hash
			);

			MessageBox.Show(
				$"Пароль пользователя «{sel.Login}» изменён.",
				"Готово",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information
			);
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Ошибка: {ex.Message}",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}
	}

	private async Task OnDeleteClick()
	{
		var sel = GetSelected();

		if (sel is null)
		{
			MessageBox.Show("Выберите пользователя.");
			return;
		}

		if (sel.Id == AppSession.UserId)
		{
			MessageBox.Show(
				"Нельзя удалить себя.",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning
			);
			return;
		}

		if (MessageBox.Show(
				$"Удалить пользователя «{sel.Login}»?",
				"Подтверждение",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning
			) != DialogResult.Yes)
		{
			return;
		}

		try
		{
			await _repo.DeleteAsync(sel.Id);
			await LoadAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Ошибка: {ex.Message}",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
		}
	}
}

public sealed class UserEditDialog : Form
{
	public string LoginValue { get; private set; } = "";
	public string PasswordValue { get; private set; } = "";
	public string FullNameValue { get; private set; } = "";
	public string EmailValue { get; private set; } = "";
	public string RoleValue { get; private set; } = "storekeeper";
	public bool IsActiveValue { get; private set; } = true;

	private readonly TextBox _tbLogin = new() { Width = 220 };
	private readonly TextBox _tbPwd = new() { Width = 220, UseSystemPasswordChar = true };
	private readonly TextBox _tbName = new() { Width = 220 };
	private readonly TextBox _tbEmail = new() { Width = 220 };

	private readonly ComboBox _cbRole = new()
	{
		Width = 220,
		DropDownStyle = ComboBoxStyle.DropDownList
	};

	private readonly CheckBox _chkActive = new()
	{
		Text = "Учётная запись активна",
		Checked = true,
		AutoSize = true
	};

	private readonly bool _isEdit;

	public UserEditDialog(User? existing)
	{
		_isEdit = existing is not null;

		Text = _isEdit
			? "Редактирование пользователя"
			: "Добавление пользователя";

		Size = new Size(360, 360);
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;

		_cbRole.Items.AddRange(new object[]
		{
			"admin",
			"storekeeper",
			"accountant"
		});

		_cbRole.SelectedIndex = 1;

		var layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 7,
			Padding = new Padding(15)
		};

		AddRow(layout, 0, "Логин:", _tbLogin);
		AddRow(layout, 1, _isEdit ? "Пароль (новый):" : "Пароль:", _tbPwd);
		AddRow(layout, 2, "ФИО:", _tbName);
		AddRow(layout, 3, "Email:", _tbEmail);
		AddRow(layout, 4, "Роль:", _cbRole);

		layout.Controls.Add(_chkActive, 1, 5);

		if (_isEdit)
		{
			_tbPwd.Enabled = false;
			_tbPwd.PlaceholderText = "(меняется отдельной кнопкой)";
		}

		var btnOk = new Button
		{
			Text = "Сохранить",
			DialogResult = DialogResult.OK,
			Width = 100
		};

		var btnCancel = new Button
		{
			Text = "Отмена",
			DialogResult = DialogResult.Cancel,
			Width = 100
		};

		btnOk.Click += OnOk;

		AcceptButton = btnOk;
		CancelButton = btnCancel;

		var btnRow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.RightToLeft
		};

		btnRow.Controls.Add(btnCancel);
		btnRow.Controls.Add(btnOk);

		layout.Controls.Add(btnRow, 1, 6);

		Controls.Add(layout);

		if (existing is not null)
		{
			_tbLogin.Text = existing.Login;
			_tbName.Text = existing.FullName;
			_tbEmail.Text = existing.Email;
			_cbRole.SelectedItem = existing.Role;
			_chkActive.Checked = existing.IsActive;
		}
	}

	private static void AddRow(
		TableLayoutPanel layout,
		int row,
		string label,
		Control control)
	{
		layout.Controls.Add(
			new Label
			{
				Text = label,
				AutoSize = true,
				Anchor = AnchorStyles.Right,
				Padding = new Padding(0, 5, 5, 0)
			},
			0,
			row
		);

		layout.Controls.Add(control, 1, row);
	}

	private void OnOk(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_tbLogin.Text) ||
			string.IsNullOrWhiteSpace(_tbName.Text) ||
			string.IsNullOrWhiteSpace(_tbEmail.Text))
		{
			MessageBox.Show(
				"Заполните логин, ФИО и email.",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning
			);

			DialogResult = DialogResult.None;
			return;
		}

		if (!_isEdit && string.IsNullOrWhiteSpace(_tbPwd.Text))
		{
			MessageBox.Show(
				"Введите пароль.",
				"Ошибка",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning
			);

			DialogResult = DialogResult.None;
			return;
		}

		LoginValue = _tbLogin.Text.Trim();
		PasswordValue = _tbPwd.Text;
		FullNameValue = _tbName.Text.Trim();
		EmailValue = _tbEmail.Text.Trim();
		RoleValue = _cbRole.SelectedItem?.ToString() ?? "storekeeper";
		IsActiveValue = _chkActive.Checked;
	}
}

public sealed class PasswordChangeDialog : Form
{
	public string NewPassword { get; private set; } = "";

	private readonly TextBox _tbPwd = new()
	{
		Width = 220,
		UseSystemPasswordChar = true
	};

	public PasswordChangeDialog()
	{
		Text = "Смена пароля";
		Size = new Size(330, 160);
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;

		var layout = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			Padding = new Padding(15)
		};

		layout.Controls.Add(new Label
		{
			Text = "Новый пароль:",
			AutoSize = true
		});

		layout.Controls.Add(_tbPwd);

		var btnOk = new Button
		{
			Text = "ОК",
			DialogResult = DialogResult.OK,
			Width = 90
		};

		var btnCancel = new Button
		{
			Text = "Отмена",
			DialogResult = DialogResult.Cancel,
			Width = 90
		};

		btnOk.Click += (_, _) => NewPassword = _tbPwd.Text;

		AcceptButton = btnOk;
		CancelButton = btnCancel;

		var btnRow = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.LeftToRight,
			AutoSize = true
		};

		btnRow.Controls.Add(btnOk);
		btnRow.Controls.Add(btnCancel);

		layout.Controls.Add(btnRow);

		Controls.Add(layout);
	}
}