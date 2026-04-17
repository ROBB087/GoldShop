using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace GoldShopWpf.Behaviors;

public static class SearchableComboBoxBehavior
{
    private sealed class SearchState
    {
        public ICollectionView? View { get; set; }
        public TextBox? SearchTextBox { get; set; }
        public string FilterText { get; set; } = string.Empty;
        public bool SuppressTextChanged { get; set; }
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SearchableComboBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty FilterMemberPathProperty =
        DependencyProperty.RegisterAttached(
            "FilterMemberPath",
            typeof(string),
            typeof(SearchableComboBoxBehavior),
            new PropertyMetadata("Name"));

    private static readonly DependencyProperty SearchStateProperty =
        DependencyProperty.RegisterAttached(
            "SearchState",
            typeof(SearchState),
            typeof(SearchableComboBoxBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OwnerComboBoxProperty =
        DependencyProperty.RegisterAttached(
            "OwnerComboBox",
            typeof(ComboBox),
            typeof(SearchableComboBoxBehavior),
            new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetFilterMemberPath(DependencyObject element, string value) => element.SetValue(FilterMemberPathProperty, value);

    public static string GetFilterMemberPath(DependencyObject element) => (string)element.GetValue(FilterMemberPathProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            comboBox.Loaded += OnComboBoxLoaded;
            comboBox.Unloaded += OnComboBoxUnloaded;
            comboBox.DropDownOpened += OnDropDownOpened;
            comboBox.DropDownClosed += OnDropDownClosed;
            comboBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            comboBox.PreviewKeyDown += OnComboBoxPreviewKeyDown;
            comboBox.PreviewMouseWheel += OnPreviewMouseWheel;
            comboBox.SelectionChanged += OnSelectionChanged;

            if (comboBox.IsLoaded)
            {
                Attach(comboBox);
            }
        }
        else
        {
            Detach(comboBox);
            comboBox.Loaded -= OnComboBoxLoaded;
            comboBox.Unloaded -= OnComboBoxUnloaded;
            comboBox.DropDownOpened -= OnDropDownOpened;
            comboBox.DropDownClosed -= OnDropDownClosed;
            comboBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            comboBox.PreviewKeyDown -= OnComboBoxPreviewKeyDown;
            comboBox.PreviewMouseWheel -= OnPreviewMouseWheel;
            comboBox.SelectionChanged -= OnSelectionChanged;
        }
    }

    private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            Attach(comboBox);
        }
    }

    private static void OnComboBoxUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            Detach(comboBox);
        }
    }

    private static void Attach(ComboBox comboBox)
    {
        comboBox.ApplyTemplate();

        var state = GetOrCreateState(comboBox);
        state.View = comboBox.ItemsSource != null
            ? CollectionViewSource.GetDefaultView(comboBox.ItemsSource)
            : CollectionViewSource.GetDefaultView(comboBox.Items);
        if (state.View != null)
        {
            state.View.Filter = item => Matches(comboBox, item, state.FilterText);
            state.View.Refresh();
        }

        if (comboBox.Template.FindName("PART_SearchTextBox", comboBox) is TextBox textBox)
        {
            if (!ReferenceEquals(state.SearchTextBox, textBox))
            {
                if (state.SearchTextBox != null)
                {
                    state.SearchTextBox.TextChanged -= OnTextBoxTextChanged;
                    state.SearchTextBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
                    state.SearchTextBox.ClearValue(OwnerComboBoxProperty);
                }

                state.SearchTextBox = textBox;
                state.SearchTextBox.SetValue(OwnerComboBoxProperty, comboBox);
                textBox.TextChanged += OnTextBoxTextChanged;
                textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            }
        }
    }

    private static void Detach(ComboBox comboBox)
    {
        var state = (SearchState?)comboBox.GetValue(SearchStateProperty);
        if (state == null)
        {
            return;
        }

        if (state.SearchTextBox != null)
        {
            state.SearchTextBox.TextChanged -= OnTextBoxTextChanged;
            state.SearchTextBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            state.SearchTextBox.ClearValue(OwnerComboBoxProperty);
        }

        if (state.View != null)
        {
            state.View.Filter = null;
            state.View.Refresh();
        }

        state.SearchTextBox = null;
        state.View = null;
        state.FilterText = string.Empty;
    }

    private static SearchState GetOrCreateState(ComboBox comboBox)
    {
        if (comboBox.GetValue(SearchStateProperty) is SearchState state)
        {
            return state;
        }

        state = new SearchState();
        comboBox.SetValue(SearchStateProperty, state);
        return state;
    }

    private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || GetOwningComboBox(textBox) is not ComboBox comboBox)
        {
            return;
        }

        var state = GetOrCreateState(comboBox);
        if (state.SuppressTextChanged)
        {
            return;
        }

        state.FilterText = textBox.Text ?? string.Empty;
        RefreshFilter(comboBox, state);

        if (!comboBox.IsDropDownOpen && comboBox.IsKeyboardFocusWithin)
        {
            comboBox.IsDropDownOpen = true;
        }
    }

    private static void OnTextBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not TextBox textBox || GetOwningComboBox(textBox) is not ComboBox comboBox)
        {
            return;
        }

        if (e.Key == System.Windows.Input.Key.Down)
        {
            e.Handled = true;
            MoveSelection(comboBox, +1);
            comboBox.Focus();
            return;
        }

        if (e.Key == System.Windows.Input.Key.Up)
        {
            e.Handled = true;
            MoveSelection(comboBox, -1);
            comboBox.Focus();
            return;
        }

        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            var searchText = GetOrCreateState(comboBox).FilterText;
            if (comboBox.SelectedItem == null || !Matches(comboBox, comboBox.SelectedItem, searchText))
            {
                SelectFirstVisibleItem(comboBox);
            }

            comboBox.IsDropDownOpen = false;
            comboBox.Focus();
            return;
        }

        if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
            comboBox.IsDropDownOpen = false;
            comboBox.Focus();
        }
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var state = GetOrCreateState(comboBox);
        if (!comboBox.IsDropDownOpen)
        {
            ClearFilter(comboBox, state);
        }
    }

    private static void OnDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var state = GetOrCreateState(comboBox);
        comboBox.Dispatcher.BeginInvoke(() =>
        {
            RefreshPopupLayout(comboBox);
            state.SearchTextBox?.Focus();
            state.SearchTextBox?.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    private static void OnDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var state = GetOrCreateState(comboBox);
        ClearFilter(comboBox, state);
    }

    private static void OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
        {
            e.Handled = true;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null)
        {
            return;
        }

        comboBox.Focus();
        comboBox.IsDropDownOpen = true;
        e.Handled = true;
    }

    private static void OnComboBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        if (e.Key == System.Windows.Input.Key.F4 ||
            e.Key == System.Windows.Input.Key.Space ||
            e.Key == System.Windows.Input.Key.Enter ||
            e.Key == System.Windows.Input.Key.Down)
        {
            comboBox.IsDropDownOpen = true;
            e.Handled = true;
        }
    }

    private static void RefreshFilter(ComboBox comboBox, SearchState state)
    {
        if (state.View == null)
        {
            return;
        }

        state.View.Refresh();
    }

    private static void ClearFilter(ComboBox comboBox, SearchState state)
    {
        if (state.View == null)
        {
            return;
        }

        if (state.SearchTextBox != null && state.SearchTextBox.Text.Length > 0)
        {
            state.SuppressTextChanged = true;
            state.SearchTextBox.Text = string.Empty;
            state.SuppressTextChanged = false;
        }

        state.FilterText = string.Empty;
        state.View.Refresh();

    }

    private static bool Matches(ComboBox comboBox, object? item, string searchText)
    {
        if (item == null || string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var itemText = GetItemText(comboBox, item);
        return itemText.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetItemText(ComboBox comboBox, object? item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        var memberPath = GetFilterMemberPath(comboBox);
        if (string.IsNullOrWhiteSpace(memberPath))
        {
            memberPath = comboBox.DisplayMemberPath;
        }

        if (!string.IsNullOrWhiteSpace(memberPath))
        {
            var property = item.GetType().GetProperty(memberPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property != null)
            {
                return property.GetValue(item)?.ToString() ?? string.Empty;
            }
        }

        return item.ToString() ?? string.Empty;
    }

    private static ComboBox? GetOwningComboBox(DependencyObject dependencyObject)
    {
        if (dependencyObject.GetValue(OwnerComboBoxProperty) is ComboBox owner)
        {
            return owner;
        }

        var current = dependencyObject;
        while (current != null)
        {
            if (current is ComboBox comboBox)
            {
                return comboBox;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? dependencyObject) where T : DependencyObject
    {
        var current = dependencyObject;
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void SelectFirstVisibleItem(ComboBox comboBox)
    {
        var state = GetOrCreateState(comboBox);
        var firstMatch = state.View?.Cast<object?>().FirstOrDefault(item => item != null);
        if (firstMatch != null)
        {
            comboBox.SelectedItem = firstMatch;
        }
    }

    private static void MoveSelection(ComboBox comboBox, int offset)
    {
        var state = GetOrCreateState(comboBox);
        if (state.View == null)
        {
            return;
        }

        var visibleItems = state.View.Cast<object?>().Where(item => item != null).ToList();
        if (visibleItems.Count == 0)
        {
            return;
        }

        var currentIndex = comboBox.SelectedItem == null ? -1 : visibleItems.IndexOf(comboBox.SelectedItem);
        var nextIndex = currentIndex < 0
            ? (offset > 0 ? 0 : visibleItems.Count - 1)
            : Math.Clamp(currentIndex + offset, 0, visibleItems.Count - 1);

        comboBox.SelectedItem = visibleItems[nextIndex];
    }

    private static void RefreshPopupLayout(ComboBox comboBox)
    {
        comboBox.ApplyTemplate();
        comboBox.UpdateLayout();

        if (comboBox.Template.FindName("PART_Popup", comboBox) is not Popup popup ||
            popup.Child is not FrameworkElement popupChild)
        {
            return;
        }

        var width = Math.Max(comboBox.ActualWidth, comboBox.MinWidth);
        popupChild.Width = width;
        popupChild.MinWidth = width;
        popupChild.InvalidateMeasure();
        popupChild.InvalidateArrange();
        popupChild.UpdateLayout();
    }
}
