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
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        Title = UiText.L("WindowAddOpeningBalanceAdjustment");
        HeaderTitleText.Text = Title;
        TypeCombo.ItemsSource = new[]
        {
            new AdjustmentOption(UiText.L("LblOpeningBalanceManufacturingAdjustment"), OpeningBalanceAdjustmentType.Manufacturing),
            new AdjustmentOption(UiText.L("LblOpeningBalanceImprovementAdjustment"), OpeningBalanceAdjustmentType.Improvement)
        };
        TypeCombo.SelectedIndex = 0;
        AdjustmentDatePicker.SelectedDate = DateTime.Today;
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
