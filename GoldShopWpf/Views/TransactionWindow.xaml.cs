using System.Globalization;
using System.Windows;
using GoldShopCore.Models;
using GoldShopCore.Services;
using GoldShopWpf.Services;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class TransactionWindow : Window
{
    private readonly bool _isReadOnly;
    private readonly decimal _defaultManufacturingPerGram;
    private readonly decimal _defaultManufacturingPerGram24;
    private readonly decimal _defaultImprovementPerGram;
    private bool _skipNextDefaultManufacturingApply;
    private bool _suspendDefaultManufacturingSync;
    private sealed record CategoryOption(string Label, string Category);
    public event EventHandler? Cancelled;

    public int SupplierId => SupplierCombo.SelectedItem is SupplierListItem s ? s.Id : 0;
    public DateTime TransactionDate => DatePicker.SelectedDate ?? DateTime.Today;
    public string TransactionCategory => (string)(TypeCombo.SelectedValue ?? TransactionCategories.GoldOutbound);
    public TransactionType TransactionType => TransactionCategories.ResolveType(TransactionCategory);
    public decimal OriginalWeight => ParseDecimal(WeightText.Text);
    public string? ItemName => string.IsNullOrWhiteSpace(ItemText.Text) ? null : ItemText.Text.Trim();
    public int OriginalKarat => KaratCombo.SelectedItem is int karat ? karat : 21;
    public decimal ManufacturingPerGram => ParseDecimal(ManufacturingText.Text);
    public decimal ImprovementPerGram => ParseDecimal(ImprovementText.Text);
    public string? Notes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public TransactionWindow(int? supplierId, IEnumerable<SupplierListItem> suppliers)
        : this(null, supplierId, suppliers, null, null, null)
    {
    }

    public TransactionWindow(int? supplierId, IEnumerable<SupplierListItem> suppliers, decimal defaultManufacturingPerGram, decimal defaultImprovementPerGram)
        : this(null, supplierId, suppliers, defaultManufacturingPerGram, null, defaultImprovementPerGram)
    {
    }

    public TransactionWindow(int? supplierId, IEnumerable<SupplierListItem> suppliers, decimal defaultManufacturingPerGram, decimal defaultManufacturingPerGram24, decimal defaultImprovementPerGram)
        : this(null, supplierId, suppliers, defaultManufacturingPerGram, defaultManufacturingPerGram24, defaultImprovementPerGram)
    {
    }

    public TransactionWindow(TransactionListItem transaction, IEnumerable<SupplierListItem> suppliers)
        : this(transaction, transaction.SupplierId, suppliers, null, null, null, false)
    {
    }

    public TransactionWindow(TransactionListItem transaction, IEnumerable<SupplierListItem> suppliers, bool isReadOnly)
        : this(transaction, transaction.SupplierId, suppliers, null, null, null, isReadOnly)
    {
    }

    private TransactionWindow(
        TransactionListItem? transaction,
        int? supplierId,
        IEnumerable<SupplierListItem> suppliers,
        decimal? defaultManufacturingPerGram,
        decimal? defaultManufacturingPerGram24,
        decimal? defaultImprovementPerGram,
        bool isReadOnly = false)
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        _isReadOnly = isReadOnly;
        _skipNextDefaultManufacturingApply = transaction != null;
        _defaultManufacturingPerGram = defaultManufacturingPerGram.GetValueOrDefault();
        _defaultManufacturingPerGram24 = defaultManufacturingPerGram24.GetValueOrDefault();
        _defaultImprovementPerGram = defaultImprovementPerGram.GetValueOrDefault();
        Title = isReadOnly
            ? UiText.L("BtnViewDetails")
            : UiText.L(transaction == null ? "WindowAddTransaction" : "WindowEditTransaction");
        HeaderTitleText.Text = Title;

        SupplierCombo.ItemsSource = suppliers;
        SupplierCombo.SelectedValue = supplierId ?? 0;
        if (SupplierCombo.SelectedIndex < 0 && SupplierCombo.Items.Count > 0)
        {
            SupplierCombo.SelectedIndex = 0;
        }

        TypeCombo.ItemsSource = BuildTypeOptions();
        KaratCombo.ItemsSource = new[] { 18, 21, 24 };

        DatePicker.SelectedDate = transaction?.Date ?? DateTime.Today;
        TypeCombo.SelectedValue = TransactionCategories.Normalize(transaction?.Category, transaction?.Type ?? TransactionType.Out);
        WeightText.Text = transaction == null || transaction.OriginalWeight == 0 ? string.Empty : transaction.OriginalWeight.ToString("0.####", CultureInfo.CurrentCulture);
        ItemText.Text = transaction?.ItemName ?? string.Empty;
        KaratCombo.SelectedItem = transaction?.OriginalKarat ?? 21;
        _suspendDefaultManufacturingSync = true;
        ManufacturingText.Text = GetManufacturingText(transaction, _defaultManufacturingPerGram);
        ImprovementText.Text = GetImprovementText(transaction, _defaultImprovementPerGram);
        _suspendDefaultManufacturingSync = false;
        NotesText.Text = transaction?.Notes ?? string.Empty;

        WeightText.TextChanged += (_, _) => UpdatePreview();
        ManufacturingText.TextChanged += (_, _) => UpdatePreview();
        ImprovementText.TextChanged += (_, _) => UpdatePreview();
        KaratCombo.SelectionChanged += (_, _) =>
        {
            ApplyDefaultManufacturingForCurrentKarat();
            UpdatePreview();
        };
        TypeCombo.SelectionChanged += (_, _) => UpdateFormForCategory();

        UpdateFormForCategory();
        ApplyReadOnlyState();
    }

    private void UpdateFormForCategory()
    {
        var category = TransactionCategory;
        var isCashPayment = category == TransactionCategories.CashPayment;
        var isLegacyGoldReceipt = category == TransactionCategories.GoldReceipt;

        WeightLabel.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;
        WeightText.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;
        ItemLabel.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;
        ItemText.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;
        KaratLabel.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;
        KaratCombo.Visibility = isCashPayment ? Visibility.Collapsed : Visibility.Visible;

        ManufacturingLabel.Visibility = isLegacyGoldReceipt ? Visibility.Collapsed : Visibility.Visible;
        ManufacturingText.Visibility = isLegacyGoldReceipt ? Visibility.Collapsed : Visibility.Visible;
        ImprovementLabel.Visibility = isLegacyGoldReceipt ? Visibility.Collapsed : Visibility.Visible;
        ImprovementText.Visibility = isLegacyGoldReceipt ? Visibility.Collapsed : Visibility.Visible;

        ManufacturingLabel.Text = isCashPayment ? UiText.L("LblManufacturingPayment") : UiText.L("LblManufacturingPerGram");
        ImprovementLabel.Text = isCashPayment ? UiText.L("LblRefiningPayment") : UiText.L("LblImprovementPerGram");

        if (isLegacyGoldReceipt)
        {
            ManufacturingText.Text = "0";
            ImprovementText.Text = "0";
        }

        if (isCashPayment)
        {
            WeightText.Text = "0";
            ItemText.Text = string.Empty;
            KaratCombo.SelectedItem = 21;
        }
        else
        {
            ApplyDefaultManufacturingForCurrentKarat();
            ApplyDefaultImprovement();
        }

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var category = TransactionCategory;
        if (category == TransactionCategories.CashPayment)
        {
            EquivalentText.Text = $"{UiText.L("LblEquivalent21")}: 0 {UiText.L("LblWeightUnit")}";
            ManufacturingTotalText.Text = $"{UiText.L("LblTotalManufacturing")}: {-ManufacturingPerGram:0.####}";
            ImprovementTotalText.Text = $"{UiText.L("LblTotalImprovement")}: {-ImprovementPerGram:0.####}";
            TraceabilityText.Text = UiText.L("MsgCashPaymentPreview");
            return;
        }

        var equivalent21 = OriginalWeight > 0 ? TransactionService.CalculateEquivalent21(OriginalWeight, OriginalKarat) : 0m;
        var supportsCharges = TransactionCategories.SupportsCharges(category) && category != TransactionCategories.CashPayment;
        var totalManufacturing = supportsCharges && OriginalWeight > 0
            ? decimal.Round(OriginalWeight * ManufacturingPerGram, 4, MidpointRounding.AwayFromZero)
            : 0m;
        var totalImprovement = supportsCharges && equivalent21 > 0
            ? decimal.Round(equivalent21 * ImprovementPerGram, 4, MidpointRounding.AwayFromZero)
            : 0m;

        if (category == TransactionCategories.FinishedGoldReceipt)
        {
            totalManufacturing = -totalManufacturing;
            totalImprovement = -totalImprovement;
        }

        EquivalentText.Text = $"{UiText.L("LblEquivalent21")}: {equivalent21:0.####} {UiText.L("LblWeightUnit")}";
        ManufacturingTotalText.Text = $"{UiText.L("LblTotalManufacturing")}: {totalManufacturing:0.####}";
        ImprovementTotalText.Text = $"{UiText.L("LblTotalImprovement")}: {totalImprovement:0.####}";
        TraceabilityText.Text = OriginalWeight > 0
            ? category == TransactionCategories.GoldReceipt
                ? UiText.L("MsgGoldReceiptPreview")
                : category == TransactionCategories.FinishedGoldReceipt
                    ? UiText.L("MsgFinishedGoldReceiptPreview")
                    : $"{UiText.L("MsgGoldConversionPrefix")} {OriginalWeight:0.####} {UiText.L("LblWeightUnit")} {OriginalKarat}K."
            : UiText.L("MsgEnterWeightPreview");
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SupplierId == 0)
        {
            MessageBox.Show(this, UiText.L("MsgSelectTrader"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryValidateDecimal(WeightText, TransactionCategory == TransactionCategories.CashPayment, "MsgWeightInvalid"))
        {
            return;
        }

        if (!TryValidateDecimal(ManufacturingText, TransactionCategory == TransactionCategories.GoldReceipt, "MsgManufacturingInvalid"))
        {
            return;
        }

        if (!TryValidateDecimal(ImprovementText, TransactionCategory == TransactionCategories.GoldReceipt, "MsgImprovementInvalid"))
        {
            return;
        }

        try
        {
            TransactionService.Validate(TransactionCategory, OriginalWeight, OriginalKarat, ManufacturingPerGram, ImprovementPerGram);
            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, UiText.LocalizeException(ex.Message), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool TryValidateDecimal(System.Windows.Controls.TextBox textBox, bool allowHidden, string resourceKey)
    {
        if (allowHidden || textBox.Visibility != Visibility.Visible)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = "0";
            return true;
        }

        if (decimal.TryParse(textBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out _))
        {
            return true;
        }

        MessageBox.Show(this, UiText.L(resourceKey), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
        textBox.Focus();
        return false;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
        DialogResult = false;
    }

    private void ApplyReadOnlyState()
    {
        if (!_isReadOnly)
        {
            return;
        }

        SupplierCombo.IsEnabled = false;
        DatePicker.IsEnabled = false;
        TypeCombo.IsEnabled = false;
        WeightText.IsReadOnly = true;
        ItemText.IsReadOnly = true;
        KaratCombo.IsEnabled = false;
        ManufacturingText.IsReadOnly = true;
        ImprovementText.IsReadOnly = true;
        NotesText.IsReadOnly = true;
        SaveButton.Visibility = Visibility.Collapsed;
    }

    private void ApplyDefaultManufacturingForCurrentKarat()
    {
        if (_skipNextDefaultManufacturingApply)
        {
            _skipNextDefaultManufacturingApply = false;
            return;
        }

        if (_suspendDefaultManufacturingSync || _isReadOnly || !TransactionCategories.SupportsCharges(TransactionCategory) || TransactionCategory == TransactionCategories.CashPayment)
        {
            return;
        }

        _suspendDefaultManufacturingSync = true;
        ManufacturingText.Text = GetDefaultManufacturingForKarat(OriginalKarat).ToString("0.####", CultureInfo.CurrentCulture);
        _suspendDefaultManufacturingSync = false;
    }

    private void ApplyDefaultImprovement()
    {
        if (_skipNextDefaultManufacturingApply || _suspendDefaultManufacturingSync || _isReadOnly || !TransactionCategories.SupportsCharges(TransactionCategory) || TransactionCategory == TransactionCategories.CashPayment)
        {
            return;
        }

        _suspendDefaultManufacturingSync = true;
        ImprovementText.Text = _defaultImprovementPerGram.ToString("0.####", CultureInfo.CurrentCulture);
        _suspendDefaultManufacturingSync = false;
    }

    private decimal GetDefaultManufacturingForKarat(int karat)
        => karat == 24 ? _defaultManufacturingPerGram24 : _defaultManufacturingPerGram;

    private static decimal ParseDecimal(string text)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;

    private static string GetManufacturingText(TransactionListItem? transaction, decimal defaultValue)
    {
        if (transaction == null)
        {
            return defaultValue.ToString("0.####", CultureInfo.CurrentCulture);
        }

        return transaction.Category == TransactionCategories.CashPayment
            ? Math.Abs(transaction.TotalManufacturing).ToString("0.####", CultureInfo.CurrentCulture)
            : transaction.ManufacturingPerGram.ToString("0.####", CultureInfo.CurrentCulture);
    }

    private static string GetImprovementText(TransactionListItem? transaction, decimal defaultValue)
    {
        if (transaction == null)
        {
            return defaultValue.ToString("0.####", CultureInfo.CurrentCulture);
        }

        return transaction.Category == TransactionCategories.CashPayment
            ? Math.Abs(transaction.TotalImprovement).ToString("0.####", CultureInfo.CurrentCulture)
            : transaction.ImprovementPerGram.ToString("0.####", CultureInfo.CurrentCulture);
    }

    private static List<CategoryOption> BuildTypeOptions()
    {
        return
        [
            new CategoryOption(UiText.L("LblGoldOutboundReport"), TransactionCategories.GoldOutbound),
            new CategoryOption(UiText.L("LblGoldReceiptReport"), TransactionCategories.GoldReceipt),
            new CategoryOption(UiText.L("LblFinishedGoldReceiptReport"), TransactionCategories.FinishedGoldReceipt),
            new CategoryOption(UiText.L("LblCashPaymentReport"), TransactionCategories.CashPayment)
        ];
    }
}
