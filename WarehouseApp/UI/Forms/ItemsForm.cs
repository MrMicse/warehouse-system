using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class ItemsForm : Form
{
    private readonly ItemRepository _repo = new();
    private readonly DataGridView _grid = new()
    {
        Dock          = DockStyle.Fill,
        ReadOnly      = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect   = false,
        AllowUserToAddRows = false,
    };
    private readonly TextBox _tbFilter = new() { PlaceholderText = "Поиск по названию или артикулу...", Width = 280 };
    private readonly Button _btnFind   = new() { Text = "Поиск",     Width = 80 };
    private readonly Button _btnAdd    = new() { Text = "Добавить",  Width = 110 };
    private readonly Button _btnEdit   = new() { Text = "Изменить",  Width = 110 };
    private readonly Button _btnDelete = new() { Text = "Удалить",   Width = 110 };
    private readonly Button _btnIn     = new() { Text = "Приход", Width = 110 };
    private readonly Button _btnOut    = new() { Text = "Расход", Width = 110 };
    private readonly Button _btnRefresh= new() { Text = "🔄",        Width = 40  };

    public ItemsForm()
    {
        Text          = "Материальные ценности";
        Size          = new Size(960, 540);
        StartPosition = FormStartPosition.CenterParent;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id",              HeaderText = "ID",         Width = 50,  Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemCode",        HeaderText = "Артикул",    Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name",            HeaderText = "Наименование", Width = 230 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName",    HeaderText = "Категория",  Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Unit",            HeaderText = "Ед.",        Width = 50  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice",       HeaderText = "Цена",       Width = 80  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CurrentQuantity", HeaderText = "Остаток",    Width = 70  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockStatus",     HeaderText = "Состояние",  Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WarehouseName",   HeaderText = "Склад",      Width = 120 });

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };
        topPanel.Controls.AddRange(new Control[] { _tbFilter, _btnFind, _btnRefresh });

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };
        bottomPanel.Controls.AddRange(new Control[]
        {
            _btnAdd, _btnEdit, _btnDelete,
            new Label { Text = "  |  ", AutoSize = true, Padding = new Padding(0, 8, 0, 0) },
            _btnIn, _btnOut,
        });

        Controls.Add(_grid);
        Controls.Add(bottomPanel);
        Controls.Add(topPanel);

        _btnAdd.Click     += async (_, _) => await OnAddClick();
        _btnEdit.Click    += async (_, _) => await OnEditClick();
        _btnDelete.Click  += async (_, _) => await OnDeleteClick();
        _btnIn.Click      += async (_, _) => await OnMovementClick("in");
        _btnOut.Click     += async (_, _) => await OnMovementClick("out");
        _btnFind.Click    += async (_, _) => await LoadAsync();
        _btnRefresh.Click += async (_, _) => { _tbFilter.Clear(); await LoadAsync(); };
        Load              += async (_, _) => await LoadAsync();

        // Бухгалтер - только просмотр и приход/расход не может, удаление недоступно
        if (AppSession.RoleName == "accountant")
        {
            _btnAdd.Enabled = _btnEdit.Enabled = _btnDelete.Enabled = false;
            _btnIn.Enabled  = _btnOut.Enabled  = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _grid.DataSource = await _repo.GetAllFromViewAsync(_tbFilter.Text.Trim());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Item? GetSelected() =>
        _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].DataBoundItem as Item;

    private async Task OnAddClick()
    {
        using var dlg = new ItemEditDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (await _repo.ExistsCodeAsync(dlg.ItemCode))
            {
                MessageBox.Show("Материал с таким артикулом уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await _repo.AddAsync(dlg.ItemCode, dlg.ItemName, dlg.Description,
                dlg.CategoryId, dlg.UnitPrice, dlg.MinQuantity, dlg.WarehouseId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка БД",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnEditClick()
    {
        var sel = GetSelected();
        if (sel is null)
        {
            MessageBox.Show("Выберите материал.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var full = await _repo.GetByIdAsync(sel.Id);
            if (full is null) return;
            using var dlg = new ItemEditDialog(full);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (await _repo.ExistsCodeAsync(dlg.ItemCode, sel.Id))
            {
                MessageBox.Show("Материал с таким артикулом уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await _repo.UpdateAsync(sel.Id, dlg.ItemCode, dlg.ItemName, dlg.Description,
                dlg.CategoryId, dlg.UnitPrice, dlg.MinQuantity, dlg.WarehouseId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка БД",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnDeleteClick()
    {
        var sel = GetSelected();
        if (sel is null)
        {
            MessageBox.Show("Выберите материал.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var confirm = MessageBox.Show(
            $"Удалить материал «{sel.Name}»?\n\nВсе движения и записи журнала будут удалены каскадно.",
            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;
        try
        {
            await _repo.DeleteAsync(sel.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка БД",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnMovementClick(string type)
    {
        var sel = GetSelected();
        if (sel is null)
        {
            MessageBox.Show("Выберите материал.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new MovementDialog(sel, type);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }
}
