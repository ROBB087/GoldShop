using System.Collections.ObjectModel;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class NotesViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private readonly List<NotesRow> _allNotes = [];

    public ObservableCollection<NotesRow> Notes { get; } = new();

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

    public RelayCommand RefreshCommand { get; }

    public NotesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        Load();
    }

    public void Load()
    {
        _allNotes.Clear();
        var suppliers = AppServices.SupplierService.GetSuppliers();
        var supplierNames = suppliers.ToDictionary(s => s.Id, s => s.Name);

        foreach (var supplier in suppliers)
        {
            if (string.IsNullOrWhiteSpace(supplier.Notes))
            {
                continue;
            }

            _allNotes.Add(new NotesRow
            {
                ClientName = supplier.Name,
                Notes = supplier.Notes!,
                Date = supplier.CreatedAt
            });
        }

        foreach (var transaction in AppServices.TransactionService.GetTransactions(null, null))
        {
            if (string.IsNullOrWhiteSpace(transaction.Notes))
            {
                continue;
            }

            _allNotes.Add(new NotesRow
            {
                ClientName = supplierNames.TryGetValue(transaction.SupplierId, out var supplierName)
                    ? supplierName
                    : $"#{transaction.SupplierId}",
                Notes = transaction.Notes!,
                Date = transaction.Date
            });
        }

        foreach (var discount in AppServices.DiscountService.GetDiscounts(null, null))
        {
            if (string.IsNullOrWhiteSpace(discount.Notes))
            {
                continue;
            }

            _allNotes.Add(new NotesRow
            {
                ClientName = supplierNames.TryGetValue(discount.SupplierId, out var supplierName)
                    ? supplierName
                    : $"#{discount.SupplierId}",
                Notes = discount.Notes!,
                Date = discount.CreatedAt
            });
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Notes.Clear();
        var query = SearchText.Trim().ToLowerInvariant();

        foreach (var row in _allNotes.OrderByDescending(n => n.Date))
        {
            if (!string.IsNullOrWhiteSpace(query) &&
                !row.ClientName.ToLowerInvariant().Contains(query) &&
                !row.Notes.ToLowerInvariant().Contains(query))
            {
                continue;
            }

            Notes.Add(row);
        }
    }
}
