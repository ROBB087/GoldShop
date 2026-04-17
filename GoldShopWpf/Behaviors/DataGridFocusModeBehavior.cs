using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoldShopWpf.Behaviors;

public static class DataGridFocusModeBehavior
{
    private sealed class FocusState
    {
        public FocusButtonAdorner? Adorner { get; set; }
        public AdornerLayer? AdornerLayer { get; set; }
        public bool IsOpening { get; set; }
    }

    private sealed class GridHostInfo
    {
        public required DependencyObject Parent { get; set; }
        public required FrameworkElement Placeholder { get; set; }
        public required Thickness OriginalMargin { get; set; }
        public required HorizontalAlignment OriginalHorizontalAlignment { get; set; }
        public required VerticalAlignment OriginalVerticalAlignment { get; set; }
        public required double OriginalWidth { get; set; }
        public required double OriginalHeight { get; set; }
        public required double OriginalMinWidth { get; set; }
        public required double OriginalMinHeight { get; set; }
        public required double OriginalMaxWidth { get; set; }
        public required double OriginalMaxHeight { get; set; }
        public int PanelIndex { get; set; }
        public double VerticalOffset { get; set; }
        public double HorizontalOffset { get; set; }
    }

    private sealed class FocusButtonAdorner : Adorner
    {
        private readonly VisualCollection _visuals;
        private readonly Button _button;

        public FocusButtonAdorner(UIElement adornedElement, Action onClick)
            : base(adornedElement)
        {
            _button = CreateButton(onClick);
            _visuals = new VisualCollection(this)
            {
                _button
            };

            IsHitTestVisible = true;
        }

        private static Button CreateButton(Action onClick)
        {
            var button = new Button
            {
                Width = 34,
                Height = 34,
                MinWidth = 34,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5DD")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                Cursor = Cursors.Hand,
                ToolTip = "Focus mode (Esc to exit)",
                Content = new TextBlock
                {
                    Text = "⛶",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#344054"))
                }
            };

            button.Click += (_, _) => onClick();
            return button;
        }

        protected override int VisualChildrenCount => _visuals.Count;

        protected override Visual GetVisualChild(int index) => _visuals[index];

        protected override Size MeasureOverride(Size constraint)
        {
            _button.Measure(constraint);
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var flowDirection = (AdornedElement as FrameworkElement)?.FlowDirection ?? FlowDirection.LeftToRight;
            var x = flowDirection == FlowDirection.RightToLeft
                ? 10
                : Math.Max(10, finalSize.Width - _button.DesiredSize.Width - 10);

            _button.Arrange(new Rect(new Point(x, 10), _button.DesiredSize));
            return finalSize;
        }
    }

    private sealed class FocusOverlayWindow : Window
    {
        public FocusOverlayWindow(DataGrid grid, Window? owner)
        {
            Owner = owner;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowState = WindowState.Maximized;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(165, 15, 23, 42));
            Focusable = true;

            var root = new Grid
            {
                Background = Brushes.Transparent
            };
            root.MouseLeftButtonDown += (_, e) =>
            {
                if (ReferenceEquals(e.OriginalSource, root))
                {
                    Close();
                }
            };

            var shell = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5DD")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Margin = new Thickness(20),
                Padding = new Thickness(0),
                SnapsToDevicePixels = true
            };

            var shellGrid = new Grid();
            shellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var topBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var topBarGrid = new Grid { FlowDirection = FlowDirection.LeftToRight };
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            topBarGrid.Children.Add(new TextBlock
            {
                Text = "Grid Focus Mode",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#344054")),
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Width = 34,
                Height = 34,
                MinWidth = 34,
                Padding = new Thickness(0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D5DD")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Colors.White),
                Content = new TextBlock
                {
                    Text = "✕",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                },
                ToolTip = "Close (Esc)"
            };
            closeButton.Click += (_, _) => Close();
            Grid.SetColumn(closeButton, 1);
            topBarGrid.Children.Add(closeButton);
            topBar.Child = topBarGrid;

            Grid.SetRow(topBar, 0);
            shellGrid.Children.Add(topBar);

            var contentHost = new Grid
            {
                Margin = new Thickness(10),
                FlowDirection = grid.FlowDirection
            };
            Grid.SetRow(contentHost, 1);
            contentHost.Children.Add(grid);
            shellGrid.Children.Add(contentHost);

            shell.Child = shellGrid;
            root.Children.Add(shell);
            Content = root;

            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    Close();
                }
            };
        }
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridFocusModeBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty FocusStateProperty =
        DependencyProperty.RegisterAttached(
            "FocusState",
            typeof(FocusState),
            typeof(DataGridFocusModeBehavior),
            new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            dataGrid.Loaded += OnDataGridLoaded;
            dataGrid.Unloaded += OnDataGridUnloaded;
            dataGrid.SizeChanged += OnDataGridSizeChanged;
            if (dataGrid.IsLoaded)
            {
                EnsureAdorner(dataGrid);
            }
        }
        else
        {
            dataGrid.Loaded -= OnDataGridLoaded;
            dataGrid.Unloaded -= OnDataGridUnloaded;
            dataGrid.SizeChanged -= OnDataGridSizeChanged;
            RemoveAdorner(dataGrid);
        }
    }

    private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            EnsureAdorner(grid);
        }
    }

    private static void OnDataGridUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            RemoveAdorner(grid);
        }
    }

    private static void OnDataGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is DataGrid grid && grid.GetValue(FocusStateProperty) is FocusState state)
        {
            state.Adorner?.InvalidateArrange();
        }
    }

    private static FocusState GetOrCreateState(DataGrid grid)
    {
        if (grid.GetValue(FocusStateProperty) is FocusState state)
        {
            return state;
        }

        state = new FocusState();
        grid.SetValue(FocusStateProperty, state);
        return state;
    }

    private static void EnsureAdorner(DataGrid grid)
    {
        var state = GetOrCreateState(grid);
        if (state.Adorner != null)
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(grid);
        if (layer == null)
        {
            grid.Dispatcher.BeginInvoke(() => EnsureAdorner(grid), DispatcherPriority.Loaded);
            return;
        }

        state.AdornerLayer = layer;
        state.Adorner = new FocusButtonAdorner(grid, () => OpenFocusMode(grid));
        layer.Add(state.Adorner);
        state.Adorner.InvalidateArrange();
    }

    private static void RemoveAdorner(DataGrid grid)
    {
        if (grid.GetValue(FocusStateProperty) is not FocusState state)
        {
            return;
        }

        if (state.Adorner != null && state.AdornerLayer != null)
        {
            state.AdornerLayer.Remove(state.Adorner);
        }

        state.Adorner = null;
        state.AdornerLayer = null;
    }

    private static void OpenFocusMode(DataGrid grid)
    {
        var state = GetOrCreateState(grid);
        if (state.IsOpening)
        {
            return;
        }

        if (!TryDetachGrid(grid, out var hostInfo))
        {
            return;
        }

        state.IsOpening = true;
        if (state.Adorner != null)
        {
            state.Adorner.Visibility = Visibility.Collapsed;
        }

        try
        {
            grid.Margin = new Thickness(0);
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.Width = double.NaN;
            grid.Height = double.NaN;
            grid.MinWidth = 0;
            grid.MinHeight = 0;
            grid.MaxWidth = double.PositiveInfinity;
            grid.MaxHeight = double.PositiveInfinity;

            var owner = Application.Current?.MainWindow ?? Window.GetWindow(hostInfo.Placeholder);
            var overlay = new FocusOverlayWindow(grid, owner);
            overlay.ShowDialog();
        }
        finally
        {
            RestoreGrid(grid, hostInfo);

            if (state.Adorner != null)
            {
                state.Adorner.Visibility = Visibility.Visible;
                state.Adorner.InvalidateArrange();
            }

            state.IsOpening = false;
        }
    }

    private static bool TryDetachGrid(DataGrid grid, out GridHostInfo hostInfo)
    {
        hostInfo = null!;
        var parent = VisualTreeHelper.GetParent(grid);
        if (parent == null)
        {
            return false;
        }

        var placeholder = new Border
        {
            MinHeight = Math.Max(220, grid.ActualHeight),
            Background = Brushes.Transparent,
            SnapsToDevicePixels = true
        };

        var scrollViewer = FindDescendant<ScrollViewer>(grid);
        var verticalOffset = scrollViewer?.VerticalOffset ?? 0;
        var horizontalOffset = scrollViewer?.HorizontalOffset ?? 0;

        var info = new GridHostInfo
        {
            Parent = parent,
            Placeholder = placeholder,
            OriginalMargin = grid.Margin,
            OriginalHorizontalAlignment = grid.HorizontalAlignment,
            OriginalVerticalAlignment = grid.VerticalAlignment,
            OriginalWidth = grid.Width,
            OriginalHeight = grid.Height,
            OriginalMinWidth = grid.MinWidth,
            OriginalMinHeight = grid.MinHeight,
            OriginalMaxWidth = grid.MaxWidth,
            OriginalMaxHeight = grid.MaxHeight,
            PanelIndex = -1,
            VerticalOffset = verticalOffset,
            HorizontalOffset = horizontalOffset
        };

        switch (parent)
        {
            case Panel panel:
                var index = panel.Children.IndexOf(grid);
                if (index < 0)
                {
                    return false;
                }

                info.PanelIndex = index;
                panel.Children.RemoveAt(index);
                panel.Children.Insert(index, placeholder);
                break;

            case Decorator decorator when ReferenceEquals(decorator.Child, grid):
                decorator.Child = placeholder;
                break;

            case ContentControl contentControl when ReferenceEquals(contentControl.Content, grid):
                contentControl.Content = placeholder;
                break;

            default:
                return false;
        }

        hostInfo = info;
        return true;
    }

    private static void RestoreGrid(DataGrid grid, GridHostInfo hostInfo)
    {
        grid.Margin = hostInfo.OriginalMargin;
        grid.HorizontalAlignment = hostInfo.OriginalHorizontalAlignment;
        grid.VerticalAlignment = hostInfo.OriginalVerticalAlignment;
        grid.Width = hostInfo.OriginalWidth;
        grid.Height = hostInfo.OriginalHeight;
        grid.MinWidth = hostInfo.OriginalMinWidth;
        grid.MinHeight = hostInfo.OriginalMinHeight;
        grid.MaxWidth = hostInfo.OriginalMaxWidth;
        grid.MaxHeight = hostInfo.OriginalMaxHeight;

        switch (hostInfo.Parent)
        {
            case Panel panel:
                var placeholderIndex = panel.Children.IndexOf(hostInfo.Placeholder);
                if (placeholderIndex >= 0)
                {
                    panel.Children.RemoveAt(placeholderIndex);
                    panel.Children.Insert(placeholderIndex, grid);
                }
                else
                {
                    var targetIndex = hostInfo.PanelIndex >= 0 && hostInfo.PanelIndex <= panel.Children.Count
                        ? hostInfo.PanelIndex
                        : panel.Children.Count;
                    panel.Children.Insert(targetIndex, grid);
                }
                break;

            case Decorator decorator:
                decorator.Child = grid;
                break;

            case ContentControl contentControl:
                contentControl.Content = grid;
                break;
        }

        grid.Dispatcher.BeginInvoke(() =>
        {
            var scrollViewer = FindDescendant<ScrollViewer>(grid);
            if (scrollViewer == null)
            {
                return;
            }

            scrollViewer.ScrollToHorizontalOffset(hostInfo.HorizontalOffset);
            scrollViewer.ScrollToVerticalOffset(hostInfo.VerticalOffset);
        }, DispatcherPriority.Loaded);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                return typed;
            }

            var nested = FindDescendant<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
