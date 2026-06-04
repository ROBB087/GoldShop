using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoldShopWpf.Views;

internal sealed class DataGridPageScrollResetter
{
    private readonly DataGrid _grid;
    private INotifyCollectionChanged? _collection;
    private bool _isResetPending;

    public DataGridPageScrollResetter(DataGrid grid)
    {
        _grid = grid;
    }

    public void Attach(INotifyCollectionChanged? collection)
    {
        if (ReferenceEquals(_collection, collection))
        {
            return;
        }

        Detach();
        _collection = collection;
        if (_collection != null)
        {
            _collection.CollectionChanged += OnCollectionChanged;
        }
    }

    public void Detach()
    {
        if (_collection != null)
        {
            _collection.CollectionChanged -= OnCollectionChanged;
            _collection = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isResetPending)
        {
            return;
        }

        _isResetPending = true;
        _ = ResetAfterLayoutAsync();
    }

    private async Task ResetAfterLayoutAsync()
    {
        var originalColumnVirtualization = _grid.EnableColumnVirtualization;
        try
        {
            _grid.EnableColumnVirtualization = false;

            await ResetAtPriorityAsync(DispatcherPriority.Loaded);
            await ResetAtPriorityAsync(DispatcherPriority.ContextIdle);
            await ResetAtPriorityAsync(DispatcherPriority.ApplicationIdle);
            await ResetAtPriorityAsync(DispatcherPriority.ApplicationIdle);
        }
        finally
        {
            _grid.EnableColumnVirtualization = originalColumnVirtualization;
            _isResetPending = false;
        }
    }

    private async Task ResetAtPriorityAsync(DispatcherPriority priority)
    {
        await _grid.Dispatcher.InvokeAsync(() =>
        {
            ClearGridState();
            _grid.UpdateLayout();

            if (FindVisualChild<ScrollViewer>(_grid) is not { } scrollViewer)
            {
                return;
            }

            scrollViewer.UpdateLayout();
            ResetScrollOffsets(scrollViewer);
        }, priority);
    }

    private void ResetScrollOffsets(ScrollViewer scrollViewer)
    {
        scrollViewer.ScrollToTop();
        if (_grid.FlowDirection == FlowDirection.RightToLeft)
        {
            scrollViewer.ScrollToLeftEnd();
            scrollViewer.UpdateLayout();
            scrollViewer.ScrollToHorizontalOffset(0);
        }
        else
        {
            scrollViewer.ScrollToLeftEnd();
            scrollViewer.UpdateLayout();
            scrollViewer.ScrollToHorizontalOffset(0);
        }
    }

    private void ClearGridState()
    {
        _grid.SelectedIndex = -1;
        _grid.SelectedItem = null;
        _grid.CurrentCell = new DataGridCellInfo();

        if (_grid.IsKeyboardFocusWithin)
        {
            Keyboard.ClearFocus();
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
