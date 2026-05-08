using WarehouseApp.Data.Repositories;

namespace WarehouseApp.UI.Forms;

public sealed class MovementsForm : Form
{
    private readonly MovementRepository _repo = new();
    private readonly DataGridView _grid = new()
    {
        Dock          = DockStyle.Fill,
        ReadOnly      = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect   = false,
        AllowUserToAddRows = false,
    };
    private readonly Button _btnRefresh = new() { Text = "Обновить", Width = 120 };

    public MovementsForm()
    {
        Text          = "Журнал движений ТМЦ";
        Size          = new Size(1100, 540);
        StartPosition = FormStartPosition.CenterParent;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id",             HeaderText = "ID",       Width = 50,  Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MovementDate",   HeaderText = "Дата",     Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TypeRu",         HeaderText = "Тип",      Width = 80  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemCode",       HeaderText = "Артикул",  Width = 90  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemName",       HeaderText = "Материал", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity",       HeaderText = "Кол-во",   Width = 70  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice",      HeaderText = "Цена",     Width = 80  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount",    HeaderText = "Сумма",    Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName",   HeaderText = "Поставщик", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WarehouseName",  HeaderText = "Склад",    Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedByUser",  HeaderText = "Кто создал", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DocumentNumber", HeaderText = "№ док.",   Width = 100 });

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(5)
        };
        bottomPanel.Controls.Add(_btnRefresh);

        Controls.Add(_grid);
        Controls.Add(bottomPanel);

        _btnRefresh.Click += async (_, _) => await LoadAsync();
        Load              += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _grid.DataSource = await _repo.GetAllAsync();
            _grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
