using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;

namespace WarehouseApp.UI.Forms;

public sealed class ItemEditDialog : Form
{
    public string  ItemCode    { get; private set; } = "";
    public string  ItemName    { get; private set; } = "";
    public string  Description { get; private set; } = "";
    public long    CategoryId  { get; private set; }
    public decimal UnitPrice   { get; private set; }
    public int     MinQuantity { get; private set; }
    public long    WarehouseId { get; private set; }

    private readonly TextBox       _tbCode  = new() { Width = 220 };
    private readonly TextBox       _tbName  = new() { Width = 220 };
    private readonly TextBox       _tbDesc  = new() { Width = 220, Multiline = true, Height = 50, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox      _cbCat   = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _numPrice= new() { Width = 220, DecimalPlaces = 2, Maximum = 99999999, Minimum = 0 };
    private readonly NumericUpDown _numMinQ = new() { Width = 220, Maximum = 999999, Minimum = 0 };
    private readonly ComboBox      _cbWh    = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };

    public ItemEditDialog(Item? existing)
    {
        Text            = existing is null ? "Добавление материала" : "Редактирование материала";
        Size            = new Size(360, 380);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8,
            Padding = new Padding(15)
        };

        AddRow(layout, 0, "Артикул:",     _tbCode);
        AddRow(layout, 1, "Наименование:",_tbName);
        AddRow(layout, 2, "Описание:",    _tbDesc);
        AddRow(layout, 3, "Категория:",   _cbCat);
        AddRow(layout, 4, "Цена:",        _numPrice);
        AddRow(layout, 5, "Мин. остаток:",_numMinQ);
        AddRow(layout, 6, "Склад:",       _cbWh);

        var btnOk     = new Button { Text = "Сохранить", DialogResult = DialogResult.OK,     Width = 100 };
        var btnCancel = new Button { Text = "Отмена",    DialogResult = DialogResult.Cancel, Width = 100 };
        btnOk.Click += OnOkClick;
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnRow.Controls.Add(btnCancel);
        btnRow.Controls.Add(btnOk);
        layout.Controls.Add(btnRow, 1, 7);

        Controls.Add(layout);

        Load += async (_, _) => await LoadComboBoxesAsync(existing);
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private async Task LoadComboBoxesAsync(Item? existing)
    {
        try
        {
            var cats = await new CategoryRepository().GetAllAsync();
            _cbCat.DataSource    = cats;
            _cbCat.ValueMember   = nameof(Category.Id);
            _cbCat.DisplayMember = nameof(Category.Name);

            var whs = await new WarehouseRepository().GetAllAsync();
            _cbWh.DataSource    = whs;
            _cbWh.ValueMember   = nameof(Warehouse.Id);
            _cbWh.DisplayMember = nameof(Warehouse.Name);

            if (existing is not null)
            {
                _tbCode.Text         = existing.ItemCode;
                _tbName.Text         = existing.Name;
                _tbDesc.Text         = existing.Description;
                _cbCat.SelectedValue = existing.CategoryId;
                _numPrice.Value      = existing.UnitPrice;
                _numMinQ.Value       = existing.MinQuantity;
                _cbWh.SelectedValue  = existing.WarehouseId;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tbCode.Text) || string.IsNullOrWhiteSpace(_tbName.Text))
        {
            MessageBox.Show("Заполните артикул и наименование.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (_cbCat.SelectedValue is null || _cbWh.SelectedValue is null)
        {
            MessageBox.Show("Выберите категорию и склад.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        ItemCode    = _tbCode.Text.Trim();
        ItemName    = _tbName.Text.Trim();
        Description = _tbDesc.Text.Trim();
        CategoryId  = Convert.ToInt64(_cbCat.SelectedValue);
        UnitPrice   = _numPrice.Value;
        MinQuantity = (int)_numMinQ.Value;
        WarehouseId = Convert.ToInt64(_cbWh.SelectedValue);
    }
}
