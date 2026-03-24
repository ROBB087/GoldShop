using System.Globalization;
using System.Windows;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class TransactionWindow : Window
{
    private sealed record TypeOption(string Label, TransactionType Type);

    public int SupplierId => SupplierCombo.SelectedItem is SupplierListItem s ? s.Id : 0;
    public DateTime TransactionDate => DatePicker.SelectedDate ?? DateTime.Today;
    public TransactionType TransactionType => (TransactionType)(TypeCombo.SelectedValue ?? TransactionType.Out);
    public decimal OriginalWeight => ParseDecimal(WeightText.Text);
    public int OriginalKarat => KaratCombo.SelectedItem is int karat ? karat : 21;
    public decimal ManufacturingPerGram => ParseDecimal(ManufacturingText.Text);
    public decimal ImprovementPerGram => ParseDecimal(ImprovementText.Text);
    public string? Notes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public TransactionWindow(int? supplierId, IEnumerable<SupplierListItem> suppliers)
        : this(null, supplierId, suppliers)
    {
    }

    public TransactionWindow(TransactionListItem transaction, IEnumerable<SupplierListItem> suppliers)
        : this(transaction, transaction.SupplierId, suppliers)
    {
    }

    private TransactionWindow(TransactionListItem? transaction, int? supplierId, IEnumerable<SupplierListItem> suppliers)
    {
        InitializeComponent();
        Title = transaction == null ? "Add Transaction" : "Edit Transaction";

        SupplierCombo.ItemsSource = suppliers.ToList();
        SupplierCombo.SelectedValue = supplierId ?? 0;
        if (SupplierCombo.SelectedIndex < 0 && SupplierCombo.Items.Count > 0)
        {
            SupplierCombo.SelectedIndex = 0;
        }

        var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
        TypeCombo.ItemsSource = new[]
        {
            new TypeOption(isArabic ? "خارج" : "OUT", TransactionType.Out),
            new TypeOption(isArabic ? "داخل" : "IN", TransactionType.In)
        };

        KaratCombo.ItemsSource = new[] { 18, 21, 22, 24 };
        DatePicker.SelectedDate = transaction?.Date ?? DateTime.Today;
        TypeCombo.SelectedValue = transaction?.Type ?? TransactionType.Out;
        WeightText.Text = transaction == null ? string.Empty : transaction.OriginalWeight.ToString("0.####", CultureInfo.CurrentCulture);
        KaratCombo.SelectedItem = transaction?.OriginalKarat ?? 21;
        ManufacturingText.Text = transaction == null ? string.Empty : transaction.ManufacturingPerGram.ToString("0.####", CultureInfo.CurrentCulture);
        ImprovementText.Text = transaction == null ? string.Empty : transaction.ImprovementPerGram.ToString("0.####", CultureInfo.CurrentCulture);
        NotesText.Text = transaction?.Notes ?? string.Empty;

        WeightText.TextChanged += (_, _) => UpdatePreview();
        ManufacturingText.TextChanged += (_, _) => UpdatePreview();
        ImprovementText.TextChanged += (_, _) => UpdatePreview();
        KaratCombo.SelectionChanged += (_, _) => UpdatePreview();

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var equivalent21 = OriginalWeight > 0 ? TransactionService.CalculateEquivalent21(OriginalWeight, OriginalKarat) : 0m;
        var totalManufacturing = OriginalWeight > 0 ? decimal.Round(OriginalWeight * ManufacturingPerGram, 4, MidpointRounding.AwayFromZero) : 0m;
        var totalImprovement = equivalent21 > 0 ? decimal.Round(equivalent21 * ImprovementPerGram, 4, MidpointRounding.AwayFromZero) : 0m;

        EquivalentText.Text = $"21K Equivalent: {equivalent21:0.####} g";
        ManufacturingTotalText.Text = $"Total Manufacturing: {totalManufacturing:0.####}";
        ImprovementTotalText.Text = $"Total Improvement: {totalImprovement:0.####}";
        TraceabilityText.Text = OriginalWeight > 0
            ? $"This value is converted from {OriginalWeight:0.####} grams of {OriginalKarat} karat."
            : "Enter weight and karat to preview the 21K conversion.";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SupplierId == 0)
        {
            MessageBox.Show(this, "Please select a trader.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            TransactionService.Validate(OriginalWeight, OriginalKarat, ManufacturingPerGram, ImprovementPerGram);
            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static decimal ParseDecimal(string text)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;
}
