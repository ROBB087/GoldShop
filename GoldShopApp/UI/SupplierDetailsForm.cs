using GoldShopCore.Models;
using GoldShopCore.Services;

namespace GoldShopApp.UI;

public class SupplierDetailsForm : Form
{
    private readonly Supplier _supplier;
    private readonly TransactionService _transactionService;

    private readonly DataGridView _grid;
    private readonly Label _balanceLabel;
    private readonly Label _summaryLabel;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ComboBox _typeFilter;
    private readonly ComboBox _categoryFilter;
    private readonly NumericUpDown _minWeight;
    private readonly NumericUpDown _maxWeight;
    private readonly TextBox _purityFilter;

    public SupplierDetailsForm(Supplier supplier, TransactionService transactionService)
    {
        _supplier = supplier;
        _transactionService = transactionService;

        Text = $"Supplier Details - {supplier.Name}";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        headerPanel.Controls.Add(new Label { Text = "Supplier:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        headerPanel.Controls.Add(new Label { Text = supplier.Name, AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        _balanceLabel = new Label { Text = "Balance: 0.00", AutoSize = true, Margin = new Padding(20, 0, 0, 0) };
        _summaryLabel = new Label { Text = string.Empty, AutoSize = true, Margin = new Padding(20, 0, 0, 0) };
        headerPanel.Controls.Add(_balanceLabel);
        headerPanel.Controls.Add(_summaryLabel);

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            WrapContents = true
        };
        filterPanel.Controls.Add(new Label { Text = "From", AutoSize = true });
        _fromDate = new DateTimePicker { Width = 120 };
        filterPanel.Controls.Add(_fromDate);
        filterPanel.Controls.Add(new Label { Text = "To", AutoSize = true });
        _toDate = new DateTimePicker { Width = 120 };
        filterPanel.Controls.Add(_toDate);

        filterPanel.Controls.Add(new Label { Text = "Type", AutoSize = true, Margin = new Padding(12, 6, 0, 0) });
        _typeFilter = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        _typeFilter.Items.Add("All");
        _typeFilter.Items.Add(TransactionType.GoldGiven);
        _typeFilter.Items.Add(TransactionType.GoldReceived);
        _typeFilter.Items.Add(TransactionType.PaymentIssued);
        _typeFilter.Items.Add(TransactionType.PaymentReceived);
        _typeFilter.SelectedIndex = 0;
        filterPanel.Controls.Add(_typeFilter);

        filterPanel.Controls.Add(new Label { Text = "Category", AutoSize = true, Margin = new Padding(12, 6, 0, 0) });
        _categoryFilter = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        _categoryFilter.Items.Add("All");
        _categoryFilter.Items.Add(TransactionCategory.Internal);
        _categoryFilter.Items.Add(TransactionCategory.External);
        _categoryFilter.SelectedIndex = 0;
        filterPanel.Controls.Add(_categoryFilter);

        filterPanel.Controls.Add(new Label { Text = "Min Weight", AutoSize = true, Margin = new Padding(12, 6, 0, 0) });
        _minWeight = new NumericUpDown { Width = 90, DecimalPlaces = 3, Maximum = 100000000 };
        filterPanel.Controls.Add(_minWeight);
        filterPanel.Controls.Add(new Label { Text = "Max Weight", AutoSize = true, Margin = new Padding(8, 6, 0, 0) });
        _maxWeight = new NumericUpDown { Width = 90, DecimalPlaces = 3, Maximum = 100000000 };
        filterPanel.Controls.Add(_maxWeight);

        filterPanel.Controls.Add(new Label { Text = "Purity", AutoSize = true, Margin = new Padding(12, 6, 0, 0) });
        _purityFilter = new TextBox { Width = 120 };
        filterPanel.Controls.Add(_purityFilter);

        var applyButton = new Button { Text = "Apply Filter", Width = 110 };
        var clearButton = new Button { Text = "Clear", Width = 80 };
        var addButton = new Button { Text = "Add Transaction", Width = 130 };
        var printButton = new Button { Text = "Print Statement", Width = 130 };
        filterPanel.Controls.Add(applyButton);
        filterPanel.Controls.Add(clearButton);
        filterPanel.Controls.Add(addButton);
        filterPanel.Controls.Add(printButton);

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "Debit" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "Credit" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Weight", HeaderText = "Weight" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Purity", HeaderText = "Purity" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance" });

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(filterPanel, 0, 1);
        mainLayout.Controls.Add(_grid, 0, 2);
        Controls.Add(mainLayout);

        applyButton.Click += (_, _) => LoadTransactions(_fromDate.Value.Date, _toDate.Value.Date);
        clearButton.Click += (_, _) => ResetFilters();
        addButton.Click += (_, _) => AddTransaction();
        printButton.Click += (_, _) => PrintStatement();

        Load += (_, _) =>
        {
            _fromDate.Value = DateTime.Today.AddMonths(-1);
            _toDate.Value = DateTime.Today;
            LoadTransactions(null, null);
        };
    }

    private void ResetFilters()
    {
        _typeFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndex = 0;
        _minWeight.Value = 0;
        _maxWeight.Value = 0;
        _purityFilter.Clear();
        LoadTransactions(null, null);
    }

    private void LoadTransactions(DateTime? from, DateTime? to)
    {
        var transactions = _transactionService.GetTransactions(_supplier.Id, from, to);

        if (_typeFilter.SelectedIndex > 0)
        {
            var selectedType = (TransactionType)_typeFilter.SelectedItem!;
            transactions = transactions.Where(t => t.Type == selectedType).ToList();
        }

        if (_categoryFilter.SelectedIndex > 0)
        {
            var selectedCategory = (TransactionCategory)_categoryFilter.SelectedItem!;
            transactions = transactions.Where(t => t.Category == selectedCategory).ToList();
        }

        if (_minWeight.Value > 0)
        {
            var minWeight = _minWeight.Value;
            transactions = transactions.Where(t => t.Weight.HasValue && t.Weight.Value >= minWeight).ToList();
        }

        if (_maxWeight.Value > 0)
        {
            var maxWeight = _maxWeight.Value;
            transactions = transactions.Where(t => t.Weight.HasValue && t.Weight.Value <= maxWeight).ToList();
        }

        if (!string.IsNullOrWhiteSpace(_purityFilter.Text))
        {
            var purity = _purityFilter.Text.Trim();
            transactions = transactions.Where(t => !string.IsNullOrWhiteSpace(t.Purity) && t.Purity.Contains(purity, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        _grid.Rows.Clear();

        decimal balance = 0m;
        foreach (var transaction in transactions)
        {
            var debit = IsIncrease(transaction.Type) ? transaction.Amount : 0m;
            var credit = IsDecrease(transaction.Type) ? transaction.Amount : 0m;
            balance += debit - credit;

            _grid.Rows.Add(
                transaction.Date.ToString("yyyy-MM-dd"),
                transaction.Type.ToString(),
                transaction.Description ?? string.Empty,
                debit == 0m ? string.Empty : debit.ToString("0.00"),
                credit == 0m ? string.Empty : credit.ToString("0.00"),
                transaction.Weight.HasValue ? transaction.Weight.Value.ToString("0.###") : string.Empty,
                transaction.Purity ?? string.Empty,
                balance.ToString("0.00")
            );
        }

        var totals = _transactionService.GetTotals(_supplier.Id, from, to);
        _balanceLabel.Text = $"Balance: {balance:0.00}";
        _summaryLabel.Text = $"Gold Given: {totals.goldGiven:0.00} | Gold Received: {totals.goldReceived:0.00} | Payments Issued: {totals.paymentsIssued:0.00} | Payments Received: {totals.paymentsReceived:0.00}";
    }

    private void AddTransaction()
    {
        using var form = new TransactionForm(_supplier);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _transactionService.AddTransaction(
                _supplier.Id,
                form.TransactionDate,
                form.TransactionType,
                form.Description,
                form.Amount,
                form.Weight,
                form.Purity,
                form.Category,
                form.Notes);
            LoadTransactions(null, null);
        }
    }

    private void PrintStatement()
    {
        using var form = new StatementForm(_supplier, _transactionService);
        form.ShowDialog(this);
    }

    private static bool IsIncrease(TransactionType type)
    {
        return type == TransactionType.GoldGiven || type == TransactionType.PaymentReceived;
    }

    private static bool IsDecrease(TransactionType type)
    {
        return type == TransactionType.GoldReceived || type == TransactionType.PaymentIssued;
    }
}
