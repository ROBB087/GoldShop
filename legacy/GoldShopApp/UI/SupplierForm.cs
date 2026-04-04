using GoldShopCore.Models;

namespace GoldShopApp.UI;

public class SupplierForm : Form
{
    private readonly TextBox _nameText;
    private readonly TextBox _phoneText;
    private readonly TextBox _workerNameText;
    private readonly TextBox _workerPhoneText;
    private readonly TextBox _notesText;

    public string SupplierName => _nameText.Text.Trim();
    public string? SupplierPhone => string.IsNullOrWhiteSpace(_phoneText.Text) ? null : _phoneText.Text.Trim();
    public string? WorkerName => string.IsNullOrWhiteSpace(_workerNameText.Text) ? null : _workerNameText.Text.Trim();
    public string? WorkerPhone => string.IsNullOrWhiteSpace(_workerPhoneText.Text) ? null : _workerPhoneText.Text.Trim();
    public string? SupplierNotes => string.IsNullOrWhiteSpace(_notesText.Text) ? null : _notesText.Text.Trim();

    public SupplierForm(Supplier? supplier = null)
    {
        Text = supplier == null ? "Add Supplier" : "Edit Supplier";
        Width = 440;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        layout.Controls.Add(new Label { Text = "Name", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _nameText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_nameText, 1, 0);

        layout.Controls.Add(new Label { Text = "Phone", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _phoneText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_phoneText, 1, 1);

        layout.Controls.Add(new Label { Text = "Worker Name", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        _workerNameText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_workerNameText, 1, 2);

        layout.Controls.Add(new Label { Text = "Worker Phone", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        _workerPhoneText = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_workerPhoneText, 1, 3);

        layout.Controls.Add(new Label { Text = "Notes", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 4);
        _notesText = new TextBox { Dock = DockStyle.Fill, Multiline = true };
        layout.Controls.Add(_notesText, 1, 4);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var okButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 5);
        layout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        if (supplier != null)
        {
            _nameText.Text = supplier.Name;
            _phoneText.Text = supplier.Phone ?? string.Empty;
            _workerNameText.Text = supplier.WorkerName ?? string.Empty;
            _workerPhoneText.Text = supplier.WorkerPhone ?? string.Empty;
            _notesText.Text = supplier.Notes ?? string.Empty;
        }

        FormClosing += OnFormClosing;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameText.Text))
        {
            MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }
}
