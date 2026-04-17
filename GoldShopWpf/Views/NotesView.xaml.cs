using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class NotesView : UserControl
{
    public NotesView()
    {
        InitializeComponent();
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is NotesViewModel viewModel)
        {
            viewModel.SetVisibleSelection(checkBox.IsChecked == true);
        }
    }
}
