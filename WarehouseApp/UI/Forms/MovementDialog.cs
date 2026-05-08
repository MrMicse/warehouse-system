using MySqlConnector;
using WarehouseApp.Data.Models;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Services;

namespace WarehouseApp.UI.Forms;

public sealed class MovementDialog : Form
{
    private readonly Item    _item;
    private readonly string  _type; // "in" / "out"
    private readonly MovementRepository _movRepo = new();

    private readonly NumericUpDown _numQty   = new() { Width = 200, Maximum = 999999, Minimum = 1, Value = 1 };
    private readonly NumericUpDown _numPrice = new() { Width = 200, DecimalPlaces = 2, Maximum = 99999999, Minimum = 0 };
    private readonly ComboBox      _cbSupp   = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox       _tbDoc    = new() { Width = 200 };
    private readonly TextBox       _tbNotes  = new() { Width = 200, Multiline = true, Height = 50 };

    public MovementDialog(Item item, string type)
    {
        _item = item;
        _type = type;

        Text            = type == "in" ? "Регистрация прихода" : "Регистрация расхода";
        Size            = new Size(360, 410);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        _numPrice.Value = item.UnitPrice;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8,
            Padding = new Padding(15)
        };

        var lblItem = new Label
        {
            Text = $"Материал: {item.Name}\nАртикул: {item.ItemCode} • Остаток: {item.CurrentQuantity} {item.Unit}",
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        };
        layout.Controls.Add(lblItem, 0, 0);
        layout.SetColumnSpan(lblItem, 2);

        AddRow(layout, 1, "Количество:",   _numQty);
        AddRow(layout, 2, "Цена за ед.:",  _numPrice);

        if (type == "in")
        {
            AddRow(layout, 3, "Поставщик:", _cbSupp);
        }
        else
        {
            _cbSupp.Visible = false;
            layout.Controls.Add(new Label { Text = "(поставщик не нужен для расхода)", AutoSize = true, ForeColor = Color.Gray }, 0, 3);
        }

        AddRow(layout, 4, "Документ №:",  _tbDoc);
        AddRow(layout, 5, "Примечание:",  _tbNotes);

        var btnOk     = new Button { Text = "Сохранить", Width = 100 };
        var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100 };
        btnOk.Click += async (s, e) => await OnOkClick(s, e);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnRow.Controls.Add(btnCancel);
        btnRow.Controls.Add(btnOk);
        layout.Controls.Add(btnRow, 1, 7);

        Controls.Add(layout);

        Load += async (_, _) => await LoadSuppliersAsync();
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 5, 5, 0) }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private async Task LoadSuppliersAsync()
    {
        if (_type != "in") return;
        try
        {
            var list = await new SupplierRepository().GetAllAsync();
            _cbSupp.DataSource    = list;
            _cbSupp.ValueMember   = nameof(Supplier.Id);
            _cbSupp.DisplayMember = nameof(Supplier.Name);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnOkClick(object? sender, EventArgs e)
    {
        try
        {
            long? supplierId = (_type == "in" && _cbSupp.SelectedValue is not null)
                ? Convert.ToInt64(_cbSupp.SelectedValue)
                : null;

            await _movRepo.RegisterAsync(
                itemId:         _item.Id,
                type:           _type,
                quantity:       (int)_numQty.Value,
                unitPrice:      _numPrice.Value,
                supplierId:     supplierId,
                warehouseId:    _item.WarehouseId,
                userId:         AppSession.UserId,
                documentNumber: _tbDoc.Text.Trim(),
                notes:          _tbNotes.Text.Trim());

            MessageBox.Show($"Операция «{(_type == "in" ? "Приход" : "Расход")}» успешно зарегистрирована.",
                "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            // Бизнес-ошибка из процедуры (например, недостаточно остатка)
            MessageBox.Show(ex.Message, "Невозможно выполнить операцию",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка БД: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.None;
        }
    }
}
