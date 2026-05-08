using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;

namespace WarehouseApp.UI.Forms;

public sealed class WarehousesForm : Form
{
    private readonly WarehouseRepository _repo = new();
    private readonly DataGridView _grid = new()
    {
        Dock          = DockStyle.Fill,
        ReadOnly      = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect   = false,
        AllowUserToAddRows = false,
    };
    private readonly Button _btnAdd    = new() { Text = "Добавить", Width = 110 };
    private readonly Button _btnEdit   = new() { Text = "Изменить", Width = 110 };
    private readonly Button _btnDelete = new() { Text = "Удалить",  Width = 110 };
    private readonly Button _btnRefresh= new() { Text = "🔄",       Width = 40  };

    public WarehousesForm()
    {
        Text          = "Склады";
        Size          = new Size(720, 440);
        StartPosition = FormStartPosition.CenterParent;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id",                  HeaderText = "ID",                  Width = 50,  Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name",                HeaderText = "Название",            Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Address",             HeaderText = "Адрес",               Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ResponsibleUserName", HeaderText = "Ответственный",      Width = 200 });

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };
        btnPanel.Controls.AddRange(new Control[] { _btnAdd, _btnEdit, _btnDelete, _btnRefresh });

        Controls.Add(_grid);
        Controls.Add(btnPanel);

        _btnAdd.Click     += async (_, _) => await OnAddClick();
        _btnEdit.Click    += async (_, _) => await OnEditClick();
        _btnDelete.Click  += async (_, _) => await OnDeleteClick();
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        Load              += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { _grid.DataSource = await _repo.GetAllAsync(); }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Warehouse? GetSelected() =>
        _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].DataBoundItem as Warehouse;

    private async Task OnAddClick()
    {
        using var dlg = new WarehouseEditDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.AddAsync(dlg.NameValue, dlg.AddressValue, dlg.ResponsibleId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnEditClick()
    {
        var sel = GetSelected();
        if (sel is null) { MessageBox.Show("Выберите склад."); return; }
        using var dlg = new WarehouseEditDialog(sel);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.UpdateAsync(sel.Id, dlg.NameValue, dlg.AddressValue, dlg.ResponsibleId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnDeleteClick()
    {
        var sel = GetSelected();
        if (sel is null) { MessageBox.Show("Выберите склад."); return; }
        if (MessageBox.Show($"Удалить склад «{sel.Name}»?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await _repo.DeleteAsync(sel.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public sealed class WarehouseEditDialog : Form
{
    public string NameValue     { get; private set; } = "";
    public string AddressValue  { get; private set; } = "";
    public long?  ResponsibleId { get; private set; }

    private readonly TextBox  _tbName = new() { Width = 240 };
    private readonly TextBox  _tbAddr = new() { Width = 240 };
    private readonly ComboBox _cbResp = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };

    public WarehouseEditDialog(Warehouse? existing)
    {
        Text            = existing is null ? "Добавление склада" : "Редактирование склада";
        Size            = new Size(380, 240);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(15) };
        layout.Controls.Add(new Label { Text = "Название:",      AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 0);
        layout.Controls.Add(_tbName, 1, 0);
        layout.Controls.Add(new Label { Text = "Адрес:",         AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 1);
        layout.Controls.Add(_tbAddr, 1, 1);
        layout.Controls.Add(new Label { Text = "Ответственный:", AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 2);
        layout.Controls.Add(_cbResp, 1, 2);

        var btnOk     = new Button { Text = "Сохранить", DialogResult = DialogResult.OK,     Width = 100 };
        var btnCancel = new Button { Text = "Отмена",    DialogResult = DialogResult.Cancel, Width = 100 };
        btnOk.Click += OnOk;
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnRow.Controls.Add(btnCancel);
        btnRow.Controls.Add(btnOk);
        layout.Controls.Add(btnRow, 1, 3);
        Controls.Add(layout);

        Load += async (_, _) =>
        {
            try
            {
                var users = await new UserRepository().GetAllAsync();
                var withEmpty = new List<Data.Models.User> { new() { Id = 0, FullName = "(не назначен)" } };
                withEmpty.AddRange(users);
                _cbResp.DataSource    = withEmpty;
                _cbResp.ValueMember   = nameof(Data.Models.User.Id);
                _cbResp.DisplayMember = nameof(Data.Models.User.FullName);

                if (existing is not null)
                {
                    _tbName.Text = existing.Name;
                    _tbAddr.Text = existing.Address;
                    _cbResp.SelectedValue = existing.ResponsibleUserId ?? 0L;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tbName.Text))
        {
            MessageBox.Show("Введите название.", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        NameValue    = _tbName.Text.Trim();
        AddressValue = _tbAddr.Text.Trim();
        var sel = _cbResp.SelectedValue is null ? 0L : Convert.ToInt64(_cbResp.SelectedValue);
        ResponsibleId = sel == 0L ? null : sel;
    }
}
