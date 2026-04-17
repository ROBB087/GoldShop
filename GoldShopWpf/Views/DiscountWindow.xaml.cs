using System.Globalization;
using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.Services;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class DiscountWindow : Window
{
    private sealed record DiscountOption(string Label, DiscountType Type);

    public DiscountType DiscountType => (DiscountType)(TypeCombo.SelectedValue ?? DiscountType.Manufacturing);
    public decimal Amount => decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0m;
    public string? Notes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public DiscountWindow()
    {
        InitializeComponent();
        DialogWindowLayout.Apply(this);
        Title = UiText.L("WindowAddDiscount");
        HeaderTitleText.Text = Title;
        TypeCombo.ItemsSource = new[]
        {
            new DiscountOption(UiText.L("LblTotalManufacturing"), DiscountType.Manufacturing),
            new DiscountOption(UiText.L("LblTotalImprovement"), DiscountType.Improvement)
        };
        TypeCombo.SelectedIndex = 0;
    }

    public DiscountWindow(DiscountListItem discount) : this()
    {
        Title = UiText.L("WindowEditDiscount", UiText.L("WindowAddDiscount"));
        HeaderTitleText.Text = Title;
        TypeCombo.SelectedValue = discount.Type;
        AmountText.Text = discount.Amount.ToString("0.####", CultureInfo.CurrentCulture);
        NotesText.Text = discount.Notes;
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
            MessageBox.Show(this, UiText.L("MsgDiscountGreaterThanZero"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            AmountText.Focus();
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
