using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class TransactionsView : UserControl
{
    private readonly DataGridPageScrollResetter _transactionsScrollResetter;

    public TransactionsView()
    {
        InitializeComponent();
        _transactionsScrollResetter = new DataGridPageScrollResetter(TransactionsGrid);
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => _transactionsScrollResetter.Detach();
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && DataContext is TransactionsViewModel viewModel)
        {
            viewModel.SetVisibleSelection(checkBox.IsChecked == true);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _transactionsScrollResetter.Attach((e.NewValue as TransactionsViewModel)?.FilteredTransactions);
    }
}
