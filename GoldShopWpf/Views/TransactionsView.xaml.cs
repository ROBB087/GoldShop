using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class TransactionsView : UserControl
{
    public TransactionsView()
    {
        InitializeComponent();
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is TransactionsViewModel viewModel)
        {
            viewModel.SetVisibleSelection(checkBox.IsChecked == true);
        }
    }
}
