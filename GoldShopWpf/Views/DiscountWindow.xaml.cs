using System.Globalization;
using System.Windows;
using GoldShopCore.Models;

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
        TypeCombo.ItemsSource = new[]
        {
            new DiscountOption("Manufacturing", DiscountType.Manufacturing),
            new DiscountOption("Improvement", DiscountType.Improvement)
        };
        TypeCombo.SelectedIndex = 0;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (Amount <= 0)
        {
            MessageBox.Show(this, "Discount amount must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
