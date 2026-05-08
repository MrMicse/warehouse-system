using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class SuppliersForm : Form
{
    private readonly SupplierRepository _repo = new();
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

    public SuppliersForm()
    {
        Text          = "Поставщики";
        Size          = new Size(820, 460);
        StartPosition = FormStartPosition.CenterParent;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id",            HeaderText = "ID",          Width = 50,  Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name",          HeaderText = "Наименование",Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ContactPerson", HeaderText = "Контактное лицо", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone",         HeaderText = "Телефон",     Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email",         HeaderText = "Email",       Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Inn",           HeaderText = "ИНН",         Width = 100 });

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };
        btnPanel.Controls.AddRange(new Control[] { _btnAdd, _btnEdit, _btnDelete, _btnRefresh });

        Controls.Add(_grid);
        Controls.Add(btnPanel);

        // Бухгалтер тоже может работать со справочником поставщиков
        if (AppSession.RoleName == "storekeeper")
        {
            _btnAdd.Enabled = _btnEdit.Enabled = _btnDelete.Enabled = false;
        }

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

    private Supplier? GetSelected() =>
        _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].DataBoundItem as Supplier;

    private async Task OnAddClick()
    {
        using var dlg = new SupplierEditDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.AddAsync(dlg.NameValue, dlg.ContactValue, dlg.PhoneValue, dlg.EmailValue, dlg.InnValue);
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
        if (sel is null) { MessageBox.Show("Выберите поставщика."); return; }
        using var dlg = new SupplierEditDialog(sel);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.UpdateAsync(sel.Id, dlg.NameValue, dlg.ContactValue, dlg.PhoneValue, dlg.EmailValue, dlg.InnValue);
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
        if (sel is null) { MessageBox.Show("Выберите поставщика."); return; }
        if (MessageBox.Show($"Удалить поставщика «{sel.Name}»?", "Подтверждение",
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

public sealed class SupplierEditDialog : Form
{
    public string NameValue    { get; private set; } = "";
    public string ContactValue { get; private set; } = "";
    public string PhoneValue   { get; private set; } = "";
    public string EmailValue   { get; private set; } = "";
    public string InnValue     { get; private set; } = "";

    private readonly TextBox _tbName = new() { Width = 240 };
    private readonly TextBox _tbCont = new() { Width = 240 };
    private readonly TextBox _tbPhon = new() { Width = 240 };
    private readonly TextBox _tbMail = new() { Width = 240 };
    private readonly TextBox _tbInn  = new() { Width = 240 };

    public SupplierEditDialog(Supplier? existing)
    {
        Text            = existing is null ? "Добавление поставщика" : "Редактирование поставщика";
        Size            = new Size(380, 320);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(15) };
        AddRow(layout, 0, "Название:",       _tbName);
        AddRow(layout, 1, "Контактное лицо:",_tbCont);
        AddRow(layout, 2, "Телефон:",        _tbPhon);
        AddRow(layout, 3, "Email:",          _tbMail);
        AddRow(layout, 4, "ИНН:",            _tbInn);

        var btnOk     = new Button { Text = "Сохранить", DialogResult = DialogResult.OK,     Width = 100 };
        var btnCancel = new Button { Text = "Отмена",    DialogResult = DialogResult.Cancel, Width = 100 };
        btnOk.Click += OnOk;
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnRow.Controls.Add(btnCancel);
        btnRow.Controls.Add(btnOk);
        layout.Controls.Add(btnRow, 1, 5);
        Controls.Add(layout);

        if (existing is not null)
        {
            _tbName.Text = existing.Name;
            _tbCont.Text = existing.ContactPerson;
            _tbPhon.Text = existing.Phone;
            _tbMail.Text = existing.Email;
            _tbInn.Text  = existing.Inn;
        }
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, row);
        layout.Controls.Add(control, 1, row);
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
        ContactValue = _tbCont.Text.Trim();
        PhoneValue   = _tbPhon.Text.Trim();
        EmailValue   = _tbMail.Text.Trim();
        InnValue     = _tbInn.Text.Trim();
    }
}
