using System.Globalization;
using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class OpeningBalanceAdjustmentWindow : Window
{
    private sealed record AdjustmentOption(string Label, OpeningBalanceAdjustmentType Type);

    public OpeningBalanceAdjustmentType AdjustmentType => (OpeningBalanceAdjustmentType)(TypeCombo.SelectedValue ?? OpeningBalanceAdjustmentType.Manufacturing);
    public DateTime AdjustmentDate => AdjustmentDatePicker.SelectedDate?.Date ?? DateTime.Today;
    public decimal Amount => decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;
    public string? Notes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public OpeningBalanceAdjustmentWindow()
        : this(null)
    {
    }

    public OpeningBalanceAdjustmentWindow(OpeningBalanceAdjustment? adjustment)
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        Title = adjustment == null
            ? UiText.L("WindowAddOpeningBalanceAdjustment")
            : UiText.L("WindowEditOpeningBalanceAdjustment", UiText.L("WindowAddOpeningBalanceAdjustment"));
        HeaderTitleText.Text = Title;
        TypeCombo.ItemsSource = new[]
        {
            new AdjustmentOption(UiText.L("LblOpeningBalanceManufacturingAdjustment"), OpeningBalanceAdjustmentType.Manufacturing),
            new AdjustmentOption(UiText.L("LblOpeningBalanceImprovementAdjustment"), OpeningBalanceAdjustmentType.Improvement)
        };
        TypeCombo.SelectedValue = adjustment?.Type ?? OpeningBalanceAdjustmentType.Manufacturing;
        AdjustmentDatePicker.SelectedDate = adjustment?.AdjustmentDate ?? DateTime.Today;
        AmountText.Text = adjustment?.Amount.ToString("0.####", CultureInfo.CurrentCulture) ?? string.Empty;
        NotesText.Text = adjustment?.Notes ?? string.Empty;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out _))
        {
            MessageBox.Show(this, UiText.L("MsgAmountInvalid"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            AmountText.Focus();
            return;
        }

        if (Amount <= 0)
        {
            MessageBox.Show(this, UiText.L("MsgOpeningBalanceAdjustmentGreaterThanZero"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            AmountText.Focus();
            return;
        }

        if (!AdjustmentDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show(this, UiText.L("MsgAdjustmentDateRequired"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            AdjustmentDatePicker.Focus();
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
