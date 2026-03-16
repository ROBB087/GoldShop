using System.Drawing.Printing;
using GoldShopCore.Models;
using GoldShopCore.Services;

namespace GoldShopApp.UI;

public class StatementForm : Form
{
    private readonly Supplier _supplier;
    private readonly TransactionService _transactionService;
    private readonly TextBox _statementText;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private string _printContent = string.Empty;

    public StatementForm(Supplier supplier, TransactionService transactionService)
    {
        _supplier = supplier;
        _transactionService = transactionService;

        Text = "Printable Supplier Statement";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        controlPanel.Controls.Add(new Label { Text = "From", AutoSize = true });
        _fromDate = new DateTimePicker { Width = 120 };
        controlPanel.Controls.Add(_fromDate);
        controlPanel.Controls.Add(new Label { Text = "To", AutoSize = true });
        _toDate = new DateTimePicker { Width = 120 };
        controlPanel.Controls.Add(_toDate);
        var generateButton = new Button { Text = "Generate", Width = 90 };
        var printButton = new Button { Text = "Print Preview", Width = 110 };
        controlPanel.Controls.Add(generateButton);
        controlPanel.Controls.Add(printButton);

        _statementText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9)
        };

        layout.Controls.Add(controlPanel, 0, 0);
        layout.Controls.Add(_statementText, 0, 1);
        Controls.Add(layout);

        generateButton.Click += (_, _) => GenerateStatement();
        printButton.Click += (_, _) => PrintStatement();

        Load += (_, _) =>
        {
            _toDate.Value = DateTime.Today;
            _fromDate.Value = DateTime.Today.AddMonths(-1);
            GenerateStatement();
        };
    }

    private void GenerateStatement()
    {
        var from = _fromDate.Value.Date;
        var to = _toDate.Value.Date;
        var transactions = _transactionService.GetTransactions(_supplier.Id, from, to);

        var lines = new List<string>
        {
            "Gold Shop Supplier Statement",
            $"Supplier: {_supplier.Name}",
            $"Date Range: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            new string('-', 110),
            string.Format("{0,-12} {1,-15} {2,-20} {3,10} {4,10} {5,8} {6,8} {7,10}", "Date", "Type", "Description", "Debit", "Credit", "Weight", "Purity", "Balance"),
            new string('-', 110)
        };

        decimal balance = 0m;
        foreach (var transaction in transactions)
        {
            var debit = IsIncrease(transaction.Type) ? transaction.Amount : 0m;
            var credit = IsDecrease(transaction.Type) ? transaction.Amount : 0m;
            balance += debit - credit;

            lines.Add(string.Format(
                "{0,-12} {1,-15} {2,-20} {3,10} {4,10} {5,8} {6,8} {7,10}",
                transaction.Date.ToString("yyyy-MM-dd"),
                transaction.Type,
                Truncate(transaction.Description, 20),
                debit == 0m ? "" : debit.ToString("0.00"),
                credit == 0m ? "" : credit.ToString("0.00"),
                transaction.Weight.HasValue ? transaction.Weight.Value.ToString("0.###") : "",
                Truncate(transaction.Purity, 8),
                balance.ToString("0.00")
            ));
        }

        lines.Add(new string('-', 110));
        lines.Add($"Final Balance: {balance:0.00}");

        _printContent = string.Join(Environment.NewLine, lines);
        _statementText.Text = _printContent;
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        return value.Length <= length ? value : value.Substring(0, length - 3) + "...";
    }

    private void PrintStatement()
    {
        if (string.IsNullOrWhiteSpace(_printContent))
        {
            GenerateStatement();
        }

        var printDocument = new PrintDocument();
        var lines = _printContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var currentLine = 0;

        printDocument.PrintPage += (_, e) =>
        {
            var font = new Font("Consolas", 9);
            var lineHeight = font.GetHeight(e.Graphics!);
            var x = e.MarginBounds.Left;
            var y = e.MarginBounds.Top;

            while (currentLine < lines.Length)
            {
                e.Graphics!.DrawString(lines[currentLine], font, Brushes.Black, x, y);
                y += (int)lineHeight;
                currentLine++;

                if (y + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;
        };

        using var preview = new PrintPreviewDialog
        {
            Document = printDocument,
            Width = 800,
            Height = 600
        };
        preview.ShowDialog(this);
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
