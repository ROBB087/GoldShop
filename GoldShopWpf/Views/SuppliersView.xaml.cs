using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is SuppliersViewModel viewModel)
        {
            viewModel.SetVisibleSelection(checkBox.IsChecked == true);
        }
    }
}
