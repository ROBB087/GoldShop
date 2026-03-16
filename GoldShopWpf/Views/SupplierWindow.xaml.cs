using System.Windows;
using GoldShopCore.Models;

namespace GoldShopWpf.Views;

public partial class SupplierWindow : Window
{
    public string SupplierName => NameText.Text.Trim();
    public string? SupplierPhone => string.IsNullOrWhiteSpace(PhoneText.Text) ? null : PhoneText.Text.Trim();
    public string? SupplierNotes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public SupplierWindow(Supplier? supplier = null)
    {
        InitializeComponent();
        Title = supplier == null ? "Add Supplier" : "Edit Supplier";

        if (supplier != null)
        {
            NameText.Text = supplier.Name;
            PhoneText.Text = supplier.Phone ?? string.Empty;
            NotesText.Text = supplier.Notes ?? string.Empty;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameText.Text))
        {
            MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
