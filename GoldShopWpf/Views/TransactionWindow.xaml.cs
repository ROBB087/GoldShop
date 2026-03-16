using System.Globalization;
using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class TransactionWindow : Window
{
    private sealed record TypeOption(string Label, TransactionType Type);
    public int SupplierId => (int)(SupplierCombo.SelectedValue ?? 0);
    public DateTime TransactionDate => DatePicker.SelectedDate ?? DateTime.Today;
    public TransactionType TransactionType => (TransactionType)(TypeCombo.SelectedValue ?? TransactionType.GoldGiven);
    public decimal Amount => decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;
    public decimal GoldWeight => decimal.TryParse(WeightText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;
    public string GoldPurity => PurityText.Text.Trim();
    public string? Notes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public string? Description
    {
        get
        {
            var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
            if (TransactionType is TransactionType.GoldGiven or TransactionType.GoldReceived)
            {
                var parts = new List<string>();
                if (GoldWeight > 0) parts.Add(isArabic ? $"وزن {GoldWeight:0.##} ج" : $"Weight {GoldWeight:0.##} g");
                if (!string.IsNullOrWhiteSpace(GoldPurity)) parts.Add(isArabic ? $"عيار {GoldPurity}" : $"Purity {GoldPurity}");
                if (parts.Count == 0)
                {
                    return TransactionType == TransactionType.GoldGiven
                        ? (isArabic ? "ذهب طالع" : "Gold given")
                        : (isArabic ? "ذهب داخل" : "Gold received");
                }
                return string.Join(", ", parts);
            }

            return TransactionType == TransactionType.PaymentIssued
                ? (isArabic ? "دفعة خارجة" : "Payment issued")
                : (isArabic ? "دفعة داخلة" : "Payment received");
        }
    }

    public TransactionWindow(int? supplierId, string supplierName, IEnumerable<SupplierListItem> suppliers)
        : this(supplierId, supplierName, suppliers, null, DateTime.Today, TransactionType.GoldGiven, string.Empty, 0m, string.Empty)
    {
    }

    public TransactionWindow(int? supplierId, string supplierName, IEnumerable<SupplierListItem> suppliers, int? id, DateTime date, TransactionType type, string details, decimal amount, string notes)
    {
        InitializeComponent();
        Title = id == null ? "Add Transaction" : "Edit Transaction";

        SupplierCombo.ItemsSource = suppliers.ToList();
        if (supplierId.HasValue)
        {
            SupplierCombo.SelectedValue = supplierId.Value;
        }
        else
        {
            SupplierCombo.SelectedIndex = 0;
        }

        var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
        TypeCombo.ItemsSource = new[]
        {
            new TypeOption(isArabic ? "ذهب طالع" : "Gold Given", TransactionType.GoldGiven),
            new TypeOption(isArabic ? "ذهب داخل" : "Gold Received", TransactionType.GoldReceived),
            new TypeOption(isArabic ? "دفعة خارجة" : "Payment Issued", TransactionType.PaymentIssued),
            new TypeOption(isArabic ? "دفعة داخلة" : "Payment Received", TransactionType.PaymentReceived)
        };

        DatePicker.SelectedDate = date;
        TypeCombo.SelectedValue = type;
        AmountText.Text = amount == 0m ? string.Empty : amount.ToString("0.00", CultureInfo.CurrentCulture);
        NotesText.Text = notes;

        if (type is TransactionType.GoldGiven or TransactionType.GoldReceived)
        {
            ParseGoldDetails(details);
        }

        UpdateGoldFieldsVisibility();
    }

    private void OnTypeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateGoldFieldsVisibility();
    }

    private void UpdateGoldFieldsVisibility()
    {
        var isGold = TransactionType is TransactionType.GoldGiven or TransactionType.GoldReceived;
        var visibility = isGold ? Visibility.Visible : Visibility.Collapsed;
        WeightLabel.Visibility = visibility;
        WeightText.Visibility = visibility;
        PurityLabel.Visibility = visibility;
        PurityText.Visibility = visibility;
    }

    private void ParseGoldDetails(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return;
        }

        if (details.Contains("Weight", StringComparison.OrdinalIgnoreCase))
        {
            var parts = details.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Weight", StringComparison.OrdinalIgnoreCase))
                {
                    var number = new string(trimmed.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray());
                    WeightText.Text = number;
                }
                else if (trimmed.StartsWith("Purity", StringComparison.OrdinalIgnoreCase))
                {
                    PurityText.Text = trimmed.Replace("Purity", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                }
            }
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SupplierId == 0)
        {
            MessageBox.Show(this, "Please select a supplier.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Amount <= 0)
        {
            MessageBox.Show(this, "Amount must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TransactionType is TransactionType.GoldGiven or TransactionType.GoldReceived && GoldWeight <= 0)
        {
            MessageBox.Show(this, "Gold weight must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
