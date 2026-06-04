using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GoldShopWpf.ViewModels;

namespace GoldShopWpf.Views;

public partial class StatementView : UserControl
{
    private StatementViewModel? _viewModel;

    public StatementView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachToViewModel(DataContext as StatementViewModel);
        UpdateNotesColumnVisibility();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachToViewModel(e.NewValue as StatementViewModel);
        UpdateNotesColumnVisibility();
    }

    private void AttachToViewModel(StatementViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StatementViewModel.ShowNotesInTable))
        {
            Dispatcher.Invoke(UpdateNotesColumnVisibility);
        }
    }

    private void UpdateNotesColumnVisibility()
    {
        if (NotesColumn == null)
        {
            return;
        }

        NotesColumn.Visibility = _viewModel?.ShowNotesInTable == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
