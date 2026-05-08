using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class LoginForm : Form
{
    private readonly AuthService _auth      = new();
    private readonly TextBox     _tbLogin   = new() { PlaceholderText = "Логин",  Width = 240 };
    private readonly TextBox     _tbPwd     = new() { PlaceholderText = "Пароль", Width = 240, UseSystemPasswordChar = true };
    private readonly Button      _btnLogin  = new() { Text = "Войти",  Width = 240, Height = 32 };
    private readonly Label       _lblHint   = new()
    {
        Text     = "Тестовые учётки:\nadmin / admin123\nstorekeeper1 / store123\naccountant1 / acc12345",
        AutoSize = true,
        ForeColor = Color.Gray
    };

    public LoginForm()
    {
        Text            = "Учёт ТМЦ — Вход в систему";
        Size            = new Size(340, 380);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        var layout = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding       = new Padding(40, 25, 0, 0),
            WrapContents  = false,
        };
        layout.Controls.Add(new Label
        {
            Text     = "Учёт материальных ценностей",
            Font     = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true,
        });
        layout.Controls.Add(new Label { Text = " ", AutoSize = true });
        layout.Controls.Add(new Label { Text = "Логин:", AutoSize = true });
        layout.Controls.Add(_tbLogin);
        layout.Controls.Add(new Label { Text = "Пароль:", AutoSize = true });
        layout.Controls.Add(_tbPwd);
        layout.Controls.Add(new Label { Text = " ", AutoSize = true });
        layout.Controls.Add(_btnLogin);
        layout.Controls.Add(new Label { Text = " ", AutoSize = true });
        layout.Controls.Add(_lblHint);
        Controls.Add(layout);

        AcceptButton    = _btnLogin;
        _btnLogin.Click += OnLoginClick;
    }

    private async void OnLoginClick(object? sender, EventArgs e)
    {
        _btnLogin.Enabled = false;
        try
        {
            var (ok, message, user) = await _auth.LoginAsync(
                _tbLogin.Text.Trim(), _tbPwd.Text);

            if (!ok || user is null)
            {
                MessageBox.Show(message, "Ошибка входа",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _tbPwd.Clear();
                _tbPwd.Focus();
                return;
            }

            AppSession.Current = user;
            var main = new MainForm();
            Hide();
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка подключения к базе данных:\n{ex.Message}\n\n" +
                            "Проверьте, что MySQL в XAMPP запущен и БД warehouse_db создана.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }
}
