using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;

namespace WarehouseApp.UI.Forms;

public sealed class CategoriesForm : Form
{
    private readonly CategoryRepository _repo = new();
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

    public CategoriesForm()
    {
        Text          = "Категории ТМЦ";
        Size          = new Size(620, 440);
        StartPosition = FormStartPosition.CenterParent;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id",          HeaderText = "ID",          Width = 50,  Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name",        HeaderText = "Название",    Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Описание",    Width = 250 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Unit",        HeaderText = "Ед. изм.",    Width = 80  });

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

    private Category? GetSelected() =>
        _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].DataBoundItem as Category;

    private async Task OnAddClick()
    {
        using var dlg = new CategoryEditDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.AddAsync(dlg.NameValue, dlg.DescriptionValue, dlg.UnitValue);
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
        if (sel is null) { MessageBox.Show("Выберите категорию."); return; }
        using var dlg = new CategoryEditDialog(sel);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _repo.UpdateAsync(sel.Id, dlg.NameValue, dlg.DescriptionValue, dlg.UnitValue);
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
        if (sel is null) { MessageBox.Show("Выберите категорию."); return; }
        if (MessageBox.Show($"Удалить категорию «{sel.Name}»?\n\nЕсли в категории есть материалы — операция не выполнится (RESTRICT).",
            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
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

public sealed class CategoryEditDialog : Form
{
    public string NameValue        { get; private set; } = "";
    public string DescriptionValue { get; private set; } = "";
    public string UnitValue        { get; private set; } = "шт.";

    private readonly TextBox _tbName = new() { Width = 220 };
    private readonly TextBox _tbDesc = new() { Width = 220 };
    private readonly TextBox _tbUnit = new() { Width = 220, Text = "шт." };

    public CategoryEditDialog(Category? existing)
    {
        Text            = existing is null ? "Добавление категории" : "Редактирование категории";
        Size            = new Size(360, 240);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(15) };
        layout.Controls.Add(new Label { Text = "Название:",  AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 0);
        layout.Controls.Add(_tbName, 1, 0);
        layout.Controls.Add(new Label { Text = "Описание:",  AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 1);
        layout.Controls.Add(_tbDesc, 1, 1);
        layout.Controls.Add(new Label { Text = "Ед. изм.:",  AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, 2);
        layout.Controls.Add(_tbUnit, 1, 2);

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

        if (existing is not null)
        {
            _tbName.Text = existing.Name;
            _tbDesc.Text = existing.Description;
            _tbUnit.Text = existing.Unit;
        }
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
        NameValue        = _tbName.Text.Trim();
        DescriptionValue = _tbDesc.Text.Trim();
        UnitValue        = string.IsNullOrWhiteSpace(_tbUnit.Text) ? "шт." : _tbUnit.Text.Trim();
    }
}
