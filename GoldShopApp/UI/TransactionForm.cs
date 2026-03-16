using GoldShopCore.Models;

namespace GoldShopApp.UI;

public class TransactionForm : Form
{
    private readonly DateTimePicker _datePicker;
    private readonly ComboBox _typeCombo;
    private readonly TextBox _descriptionText;
    private readonly NumericUpDown _amountNumeric;
    private readonly NumericUpDown _weightNumeric;
    private readonly TextBox _purityText;
    private readonly ComboBox _categoryCombo;
    private readonly TextBox _notesText;

    public DateTime TransactionDate => _datePicker.Value.Date;
    public TransactionType TransactionType => (TransactionType)_typeCombo.SelectedItem!;
    public string? Description => string.IsNullOrWhiteSpace(_descriptionText.Text) ? null : _descriptionText.Text.Trim();
    public decimal Amount => _amountNumeric.Value;
    public decimal? Weight => _weightNumeric.Enabled ? _weightNumeric.Value : null;
    public string? Purity => _purityText.Enabled && !string.IsNullOrWhiteSpace(_purityText.Text) ? _purityText.Text.Trim() : null;
    public TransactionCategory Category => _categoryCombo.Enabled ? (TransactionCategory)_categoryCombo.SelectedItem! : TransactionCategory.None;
    public string? Notes => string.IsNullOrWhiteSpace(_notesText.Text) ? null : _notesText.Text.Trim();

    public TransactionForm(Supplier supplier)
    {
        Text = $"Add Transaction - {supplier.Name}";
        Width = 520;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        layout.Controls.Add(new Label { Text = "Date", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _datePicker = new DateTimePicker { Dock = DockStyle.Fill };
        layout.Controls.Add(_datePicker, 1, 0);

        layout.Controls.Add(new Label { Text = "Type", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _typeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _typeCombo.Items.Add(TransactionType.GoldGiven);
        _typeCombo.Items.Add(TransactionType.GoldReceived);
        _typeCombo.Items.Add(TransactionType.PaymentIssued);
        _typeCombo.Items.Add(TransactionType.PaymentReceived);
        _typeCombo.SelectedIndex = 0;
        layout.Controls.Add(_typeCombo, 1, 1);

        layout.Controls.Add(new Label { Text = "Description", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        _descriptionText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_descriptionText, 1, 2);

        layout.Controls.Add(new Label { Text = "Amount", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        _amountNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000000, DecimalPlaces = 2 };
        layout.Controls.Add(_amountNumeric, 1, 3);

        layout.Controls.Add(new Label { Text = "Weight (g)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 4);
        _weightNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000000, DecimalPlaces = 3 };
        layout.Controls.Add(_weightNumeric, 1, 4);

        layout.Controls.Add(new Label { Text = "Purity / Karat", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
        _purityText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_purityText, 1, 5);

        layout.Controls.Add(new Label { Text = "Category", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
        _categoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _categoryCombo.Items.Add(TransactionCategory.Internal);
        _categoryCombo.Items.Add(TransactionCategory.External);
        _categoryCombo.SelectedIndex = 0;
        layout.Controls.Add(_categoryCombo, 1, 6);

        layout.Controls.Add(new Label { Text = "Notes", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 7);
        _notesText = new TextBox { Dock = DockStyle.Fill, Multiline = true };
        layout.Controls.Add(_notesText, 1, 7);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var okButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 8);
        layout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        FormClosing += OnFormClosing;
        _typeCombo.SelectedIndexChanged += (_, _) => UpdateTypeFields();
        UpdateTypeFields();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            return;
        }

        if (_amountNumeric.Value <= 0)
        {
            MessageBox.Show(this, "Amount must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        if (IsGoldType(TransactionType) && _weightNumeric.Value <= 0)
        {
            MessageBox.Show(this, "Weight must be greater than zero for gold transactions.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }

    private void UpdateTypeFields()
    {
        var isGold = IsGoldType(TransactionType);
        _weightNumeric.Enabled = isGold;
        _purityText.Enabled = isGold;
        _categoryCombo.Enabled = isGold;
        if (!isGold)
        {
            _weightNumeric.Value = 0;
            _purityText.Clear();
            _categoryCombo.SelectedIndex = 0;
        }
    }

    private static bool IsGoldType(TransactionType type)
    {
        return type == TransactionType.GoldGiven || type == TransactionType.GoldReceived;
    }
}
