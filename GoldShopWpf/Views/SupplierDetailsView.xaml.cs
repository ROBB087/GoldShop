using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class SupplierDetailsView : UserControl
{
    private readonly DataGridPageScrollResetter _transactionsScrollResetter;
    private readonly DataGridPageScrollResetter _discountsScrollResetter;

    public SupplierDetailsView()
    {
        InitializeComponent();
        _transactionsScrollResetter = new DataGridPageScrollResetter(TransactionsGrid);
        _discountsScrollResetter = new DataGridPageScrollResetter(DiscountsGrid);
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) =>
        {
            _transactionsScrollResetter.Detach();
            _discountsScrollResetter.Detach();
        };
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

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SupplierDetailsViewModel viewModel)
        {
            _transactionsScrollResetter.Attach(viewModel.Transactions);
            _discountsScrollResetter.Attach(viewModel.Discounts);
        }
        else
        {
            _transactionsScrollResetter.Detach();
            _discountsScrollResetter.Detach();
        }
    }
}
