using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class SupplierDetailsView : UserControl
{
    public SupplierDetailsView()
    {
        InitializeComponent();
    }

    private void OnSelectAllTransactionsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is SupplierDetailsViewModel viewModel)
        {
            viewModel.SetVisibleTransactionSelection(checkBox.IsChecked == true);
        }
    }

    private void OnSelectAllDiscountsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is SupplierDetailsViewModel viewModel)
        {
            viewModel.SetVisibleDiscountSelection(checkBox.IsChecked == true);
        }
    }
}
