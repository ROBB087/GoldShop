using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class SupplierDetailsView : UserControl
{
    public SupplierDetailsView()
    {
        InitializeComponent();
    }

    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.DataContext is not TransactionRow transaction)
        {
            return;
        }

        if (IsInteractiveChild(e.OriginalSource as DependencyObject))
        {
            return;
        }

        transaction.IsSelected = !transaction.IsSelected;
        row.IsSelected = transaction.IsSelected;
        e.Handled = true;
    }

    private static bool IsInteractiveChild(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is CheckBox || source is Button || source is MenuItem || source is TextBox || source is ComboBox)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void OnSelectAllTransactionsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is SupplierDetailsViewModel viewModel)
        {
            viewModel.SetVisibleTransactionSelection(checkBox.IsChecked == true);
        }
    }
}
