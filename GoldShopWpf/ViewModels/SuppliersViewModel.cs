using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class SuppliersViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;
    private bool _isCompactDensity;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<SupplierListItem> FilteredSuppliers { get; } = new();
    public bool? AreAllVisibleSelected
    {
        get
        {
            if (FilteredSuppliers.Count == 0)
            {
                return false;
            }

            var selectedCount = FilteredSuppliers.Count(supplier => supplier.IsSelected);
            return selectedCount == FilteredSuppliers.Count;
        }
        set
        {
            var shouldSelect = value == true;

            foreach (var supplier in FilteredSuppliers)
            {
                supplier.IsSelected = shouldSelect;
            }

            RefreshSelectionState();
        }
    }

    public int CheckedCount => Suppliers.Count(supplier => supplier.IsSelected);
    public int EffectiveSelectedCount => CheckedCount > 0 ? CheckedCount : SelectedSupplier == null ? 0 : 1;
    public int TotalSuppliers => Suppliers.Count;
    public int VisibleSuppliersCount => FilteredSuppliers.Count;
    public bool HasSelection => CheckedCount > 0 || SelectedSupplier != null;
    public string SelectedCountLabel => UiText.Format("LblSelectedCount", EffectiveSelectedCount);
    public double TableRowHeight => IsCompactDensity ? 40 : 52;
    public double TableHeaderHeight => IsCompactDensity ? 40 : 48;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public SupplierListItem? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                RefreshSelectionState();
            }
        }
    }

    public bool IsCompactDensity
    {
        get => _isCompactDensity;
        set
        {
            if (SetProperty(ref _isCompactDensity, value))
            {
                OnPropertyChanged(nameof(TableRowHeight));
                OnPropertyChanged(nameof(TableHeaderHeight));
            }
        }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand OpenDetailsCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand EditRowCommand { get; }
    public RelayCommand DeleteRowCommand { get; }
    public RelayCommand OpenRowDetailsCommand { get; }

    public event Action<SupplierListItem>? OpenDetailsRequested;

    public SuppliersViewModel()
    {
        Suppliers.CollectionChanged += OnSuppliersCollectionChanged;
        RefreshCommand = new RelayCommand(_ => Load());
        AddCommand = new RelayCommand(_ => AddSupplier());
        EditCommand = new RelayCommand(_ => EditSupplier(null), _ => GetEditableSupplier(null) != null);
        DeleteCommand = new RelayCommand(_ => DeleteSuppliers(null), _ => GetDeleteTargets(null).Count > 0);
        OpenDetailsCommand = new RelayCommand(_ => OpenDetails(null), _ => GetEditableSupplier(null) != null);
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => HasSelection);
        EditRowCommand = new RelayCommand(parameter => EditSupplier(parameter), parameter => GetEditableSupplier(parameter) != null);
        DeleteRowCommand = new RelayCommand(parameter => DeleteSuppliers(parameter), parameter => GetDeleteTargets(parameter).Count > 0);
        OpenRowDetailsCommand = new RelayCommand(parameter => OpenDetails(parameter), parameter => GetEditableSupplier(parameter) != null);

        Load();
    }

    public void Load()
    {
        Suppliers.Clear();
        FilteredSuppliers.Clear();
        SelectedSupplier = null;

        var supplierService = AppServices.SupplierService;
        var totalGoldBySupplier = supplierService.GetTotalGold21BySupplier();
        var netGoldBySupplier = supplierService.GetNetGold21BySupplier();
        var lastDates = supplierService.GetLastTransactionDates();
        var noActivityLabel = System.Windows.Application.Current.TryFindResource("LblNoActivity")?.ToString() ?? "No activity";

        foreach (var supplier in supplierService.GetSuppliers())
        {
            totalGoldBySupplier.TryGetValue(supplier.Id, out var totalGold21);
            netGoldBySupplier.TryGetValue(supplier.Id, out var netGold21);
            lastDates.TryGetValue(supplier.Id, out var lastDate);
            Suppliers.Add(new SupplierListItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone ?? string.Empty,
                WorkerName = supplier.WorkerName ?? string.Empty,
                WorkerPhone = supplier.WorkerPhone ?? string.Empty,
                TotalGold21 = totalGold21,
                NetGold21 = netGold21,
                LastTransactionDate = lastDate == default ? null : lastDate,
                LastActivityLabel = lastDate == default ? noActivityLabel : lastDate.ToString("yyyy/MM/dd hh:mm tt")
            });
        }

        ApplyFilter();
        OnPropertyChanged(nameof(TotalSuppliers));
        RefreshSelectionState();
    }

    public void SetVisibleSelection(bool isSelected)
    {
        foreach (var supplier in FilteredSuppliers)
        {
            supplier.IsSelected = isSelected;
        }

        RefreshSelectionState();
    }

    private void ApplyFilter()
    {
        FilteredSuppliers.Clear();
        var query = SearchText.Trim().ToLowerInvariant();

        foreach (var supplier in Suppliers)
        {
            if (string.IsNullOrWhiteSpace(query) ||
                supplier.Name.ToLowerInvariant().Contains(query) ||
                supplier.Phone.ToLowerInvariant().Contains(query))
            {
                FilteredSuppliers.Add(supplier);
            }
        }

        OnPropertyChanged(nameof(VisibleSuppliersCount));
        RefreshSelectionState();
    }

    private void AddSupplier()
    {
        var dialog = new Views.SupplierWindow();
        if (dialog.ShowDialog() == true)
        {
            AppServices.SupplierService.AddSupplier(dialog.SupplierName, dialog.SupplierPhone, dialog.WorkerName, dialog.WorkerPhone, dialog.SupplierNotes);
            Load();
            ToastService.ShowSuccess(UiText.L("MsgSupplierSaved"));
        }
    }

    private void EditSupplier(object? parameter)
    {
        var editableSupplier = GetEditableSupplier(parameter);
        if (editableSupplier == null)
        {
            return;
        }

        var supplier = AppServices.SupplierService.GetSupplier(editableSupplier.Id);
        if (supplier == null)
        {
            return;
        }

        var dialog = new Views.SupplierWindow(supplier);
        if (dialog.ShowDialog() == true)
        {
            AppServices.SupplierService.UpdateSupplier(supplier.Id, dialog.SupplierName, dialog.SupplierPhone, dialog.WorkerName, dialog.WorkerPhone, dialog.SupplierNotes);
            Load();
            ToastService.ShowSuccess(UiText.L("MsgSupplierSaved"));
        }
    }

    private void DeleteSuppliers(object? parameter)
    {
        var deleteTargets = GetDeleteTargets(parameter);
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = GetDeleteConfirmationMessage(deleteTargets, FilteredSuppliers.Count);
        var result = System.Windows.MessageBox.Show(
            message,
            UiText.L("TitleConfirmDelete"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            foreach (var supplier in deleteTargets)
            {
                AppServices.SupplierService.DeleteSupplier(supplier.Id);
            }

            Load();
            ToastService.ShowSuccess(UiText.L("MsgSupplierDeleted"));
        }
    }

    private void OpenDetails(object? parameter)
    {
        var editableSupplier = GetEditableSupplier(parameter);
        if (editableSupplier == null)
        {
            return;
        }

        OpenDetailsRequested?.Invoke(editableSupplier);
    }

    private void ClearSelection()
    {
        foreach (var supplier in Suppliers)
        {
            supplier.IsSelected = false;
        }

        SelectedSupplier = null;
        RefreshSelectionState();
    }

    private void OnSuppliersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<SupplierListItem>())
            {
                item.PropertyChanged -= OnSupplierPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<SupplierListItem>())
            {
                item.PropertyChanged += OnSupplierPropertyChanged;
            }
        }
    }

    private void OnSupplierPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SupplierListItem.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    private SupplierListItem? GetEditableSupplier(object? parameter)
    {
        if (parameter is SupplierListItem supplier)
        {
            return supplier;
        }

        var checkedSuppliers = Suppliers.Where(supplier => supplier.IsSelected).ToList();
        if (checkedSuppliers.Count == 1)
        {
            return checkedSuppliers[0];
        }

        if (checkedSuppliers.Count > 1)
        {
            return null;
        }

        return SelectedSupplier;
    }

    private List<SupplierListItem> GetDeleteTargets(object? parameter)
    {
        if (parameter is SupplierListItem supplier)
        {
            return [supplier];
        }

        var checkedSuppliers = Suppliers.Where(supplier => supplier.IsSelected).ToList();
        if (checkedSuppliers.Count > 0)
        {
            return checkedSuppliers;
        }

        return SelectedSupplier == null ? [] : [SelectedSupplier];
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleSelected));
        OnPropertyChanged(nameof(CheckedCount));
        OnPropertyChanged(nameof(EffectiveSelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedCountLabel));
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        OpenDetailsCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
        EditRowCommand.RaiseCanExecuteChanged();
        DeleteRowCommand.RaiseCanExecuteChanged();
        OpenRowDetailsCommand.RaiseCanExecuteChanged();
    }

    private string GetDeleteConfirmationMessage(List<SupplierListItem> targets, int visibleCount)
    {
        if (targets.Count == 0)
        {
            return string.Empty;
        }

        if (targets.Count == 1)
        {
            return UiText.Format("MsgDeleteTraderConfirm", targets[0].Name);
        }

        if (targets.Count == Suppliers.Count && Suppliers.Count > 0)
        {
            var visibleNames = string.Join("، ", targets.Take(5).Select(t => t.Name));
            return $"{UiText.L("MsgDeleteAllRecordsConfirm")}{Environment.NewLine}{Environment.NewLine}{visibleNames}";
        }

        var names = string.Join("، ", targets.Take(5).Select(t => t.Name));
        var suffix = targets.Count > 5 ? "..." : string.Empty;
        return $"{UiText.Format("MsgDeleteSelectedRecordsConfirm", targets.Count)}{Environment.NewLine}{Environment.NewLine}{names}{suffix}";
    }
}
