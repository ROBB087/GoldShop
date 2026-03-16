using GoldShopCore.Services;

namespace GoldShopApp.UI;

public class WeeklyReportForm : Form
{
    private readonly ReportService _reportService;
    private readonly DataGridView _grid;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;

    public WeeklyReportForm(ReportService reportService)
    {
        _reportService = reportService;

        Text = "Weekly Report";
        Width = 1100;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        filterPanel.Controls.Add(new Label { Text = "From", AutoSize = true });
        _fromDate = new DateTimePicker { Width = 120 };
        filterPanel.Controls.Add(_fromDate);
        filterPanel.Controls.Add(new Label { Text = "To", AutoSize = true });
        _toDate = new DateTimePicker { Width = 120 };
        filterPanel.Controls.Add(_toDate);
        var refreshButton = new Button { Text = "Refresh", Width = 90 };
        filterPanel.Controls.Add(refreshButton);

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Supplier", HeaderText = "Supplier" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GoldGiven", HeaderText = "Gold Given" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GoldReceived", HeaderText = "Gold Received" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentsIssued", HeaderText = "Payments Issued" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentsReceived", HeaderText = "Payments Received" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Current Balance" });

        layout.Controls.Add(filterPanel, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        Controls.Add(layout);

        refreshButton.Click += (_, _) => LoadReport();

        Load += (_, _) =>
        {
            _toDate.Value = DateTime.Today;
            _fromDate.Value = DateTime.Today.AddDays(-7);
            LoadReport();
        };
    }

    private void LoadReport()
    {
        var reports = _reportService.GetWeeklyReport(_fromDate.Value.Date, _toDate.Value.Date);
        _grid.Rows.Clear();

        foreach (var report in reports)
        {
            _grid.Rows.Add(
                report.SupplierName,
                report.GoldGiven.ToString("0.00"),
                report.GoldReceived.ToString("0.00"),
                report.PaymentsIssued.ToString("0.00"),
                report.PaymentsReceived.ToString("0.00"),
                report.CurrentBalance.ToString("0.00")
            );
        }
    }
}
