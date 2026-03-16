using System.Collections.ObjectModel;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class SuppliersViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private SupplierListItem? _selectedSupplier;

    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<SupplierListItem> FilteredSuppliers { get; } = new();

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
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                OpenDetailsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand OpenDetailsCommand { get; }

    public event Action<SupplierListItem>? OpenDetailsRequested;

    public SuppliersViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        AddCommand = new RelayCommand(_ => AddSupplier());
        EditCommand = new RelayCommand(_ => EditSupplier(), _ => SelectedSupplier != null);
        DeleteCommand = new RelayCommand(_ => DeleteSupplier(), _ => SelectedSupplier != null);
        OpenDetailsCommand = new RelayCommand(_ => OpenDetails(), _ => SelectedSupplier != null);

        Load();
    }

    public void Load()
    {
        Suppliers.Clear();
        FilteredSuppliers.Clear();

        var supplierService = AppServices.SupplierService;
        var balances = supplierService.GetBalancesBySupplier();
        var lastDates = supplierService.GetLastTransactionDates();
        var noActivityLabel = System.Windows.Application.Current.TryFindResource("LblNoActivity")?.ToString() ?? "No activity";

        foreach (var supplier in supplierService.GetSuppliers())
        {
            balances.TryGetValue(supplier.Id, out var balance);
            lastDates.TryGetValue(supplier.Id, out var lastDate);
            Suppliers.Add(new SupplierListItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone ?? string.Empty,
                Balance = balance,
                LastTransactionDate = lastDate == default ? null : lastDate,
                LastActivityLabel = lastDate == default ? noActivityLabel : lastDate.ToString("yyyy-MM-dd")
            });
        }

        ApplyFilter();
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
    }

    private void AddSupplier()
    {
        var dialog = new Views.SupplierWindow();
        if (dialog.ShowDialog() == true)
        {
            AppServices.SupplierService.AddSupplier(dialog.SupplierName, dialog.SupplierPhone, dialog.SupplierNotes);
            Load();
        }
    }

    private void EditSupplier()
    {
        if (SelectedSupplier == null)
        {
            return;
        }

        var supplier = AppServices.SupplierService.GetSupplier(SelectedSupplier.Id);
        if (supplier == null)
        {
            return;
        }

        var dialog = new Views.SupplierWindow(supplier);
        if (dialog.ShowDialog() == true)
        {
            AppServices.SupplierService.UpdateSupplier(supplier.Id, dialog.SupplierName, dialog.SupplierPhone, dialog.SupplierNotes);
            Load();
        }
    }

    private void DeleteSupplier()
    {
        if (SelectedSupplier == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Delete supplier {SelectedSupplier.Name}? This will remove all transactions.",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            AppServices.SupplierService.DeleteSupplier(SelectedSupplier.Id);
            Load();
        }
    }

    private void OpenDetails()
    {
        if (SelectedSupplier == null)
        {
            return;
        }

        OpenDetailsRequested?.Invoke(SelectedSupplier);
    }
}
