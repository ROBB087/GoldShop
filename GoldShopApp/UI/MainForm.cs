using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopCore.Data;

namespace GoldShopApp.UI;

public class MainForm : Form
{
    private readonly SupplierService _supplierService;
    private readonly TransactionService _transactionService;
    private readonly ReportService _reportService;

    private readonly DataGridView _grid;
    private readonly Button _addButton;
    private readonly Button _editButton;
    private readonly Button _detailsButton;
    private readonly Button _weeklyReportButton;
    private readonly Button _backupButton;
    private readonly Button _refreshButton;
    private readonly Label _summaryLabel;

    private List<Supplier> _suppliers = new();

    public MainForm(SupplierService supplierService, TransactionService transactionService, ReportService reportService)
    {
        _supplierService = supplierService;
        _transactionService = transactionService;
        _reportService = reportService;

        Text = "Gold Shop Supplier Accounting";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var summaryPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        _summaryLabel = new Label { Text = "Totals: 0.00", AutoSize = true };
        summaryPanel.Controls.Add(_summaryLabel);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        _addButton = new Button { Text = "Add Supplier", Width = 120 };
        _editButton = new Button { Text = "Edit Supplier", Width = 120 };
        _detailsButton = new Button { Text = "Details", Width = 100 };
        _weeklyReportButton = new Button { Text = "Weekly Report", Width = 120 };
        _backupButton = new Button { Text = "Backup DB", Width = 100 };
        _refreshButton = new Button { Text = "Refresh", Width = 90 };

        buttonPanel.Controls.AddRange(new Control[]
        {
            _addButton, _editButton, _detailsButton, _weeklyReportButton, _backupButton, _refreshButton
        });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false
        };
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Phone", SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", SortMode = DataGridViewColumnSortMode.NotSortable });

        mainLayout.Controls.Add(summaryPanel, 0, 0);
        mainLayout.Controls.Add(buttonPanel, 0, 1);
        mainLayout.Controls.Add(_grid, 0, 2);
        Controls.Add(mainLayout);

        _addButton.Click += (_, _) => AddSupplier();
        _editButton.Click += (_, _) => EditSupplier();
        _detailsButton.Click += (_, _) => OpenDetails();
        _weeklyReportButton.Click += (_, _) => OpenWeeklyReport();
        _backupButton.Click += (_, _) => BackupDatabase();
        _refreshButton.Click += (_, _) => LoadSuppliers();
        _grid.CellDoubleClick += (_, _) => OpenDetails();

        Load += (_, _) => LoadSuppliers();
    }

    private void LoadSuppliers()
    {
        _suppliers = _supplierService.GetSuppliers();
        var balances = _supplierService.GetBalancesBySupplier();
        var totals = _transactionService.GetTotalsAll(null, null);

        _grid.Rows.Clear();
        foreach (var supplier in _suppliers)
        {
            balances.TryGetValue(supplier.Id, out var balance);
            var rowIndex = _grid.Rows.Add(supplier.Name, supplier.Phone ?? string.Empty, balance.ToString("0.00"));
            _grid.Rows[rowIndex].Tag = supplier.Id;
        }

        _summaryLabel.Text = $"Totals | Gold Given: {totals.goldGiven:0.00} | Gold Received: {totals.goldReceived:0.00} | Payments Issued: {totals.paymentsIssued:0.00} | Payments Received: {totals.paymentsReceived:0.00}";
    }

    private Supplier? GetSelectedSupplier()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            return null;
        }

        var selectedRow = _grid.SelectedRows[0];
        if (selectedRow.Tag is int supplierId)
        {
            return _suppliers.FirstOrDefault(s => s.Id == supplierId);
        }

        return null;
    }

    private void AddSupplier()
    {
        using var form = new SupplierForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _supplierService.AddSupplier(form.SupplierName, form.SupplierPhone, form.SupplierNotes);
            LoadSuppliers();
        }
    }

    private void EditSupplier()
    {
        var supplier = GetSelectedSupplier();
        if (supplier == null)
        {
            MessageBox.Show(this, "Select a supplier first.", "Edit Supplier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new SupplierForm(supplier);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _supplierService.UpdateSupplier(supplier.Id, form.SupplierName, form.SupplierPhone, form.SupplierNotes);
            LoadSuppliers();
        }
    }

    private void OpenDetails()
    {
        var supplier = GetSelectedSupplier();
        if (supplier == null)
        {
            MessageBox.Show(this, "Select a supplier first.", "Supplier Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new SupplierDetailsForm(supplier, _transactionService);
        form.ShowDialog(this);
        LoadSuppliers();
    }

    private void OpenWeeklyReport()
    {
        using var form = new WeeklyReportForm(_reportService);
        form.ShowDialog(this);
    }

    private void BackupDatabase()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Database Backup",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            FileName = $"goldshop-backup-{DateTime.Now:yyyyMMdd}.db"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.Copy(Database.DbFilePath, dialog.FileName, true);
            MessageBox.Show(this, "Backup created.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
