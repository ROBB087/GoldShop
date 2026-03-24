using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class SupplierWindow : Window
{
    public string SupplierName => NameText.Text.Trim();
    public string? SupplierPhone => string.IsNullOrWhiteSpace(PhoneText.Text) ? null : PhoneText.Text.Trim();
    public string? WorkerName => string.IsNullOrWhiteSpace(WorkerNameText.Text) ? null : WorkerNameText.Text.Trim();
    public string? WorkerPhone => string.IsNullOrWhiteSpace(WorkerPhoneText.Text) ? null : WorkerPhoneText.Text.Trim();
    public string? SupplierNotes => string.IsNullOrWhiteSpace(NotesText.Text) ? null : NotesText.Text.Trim();

    public SupplierWindow(Supplier? supplier = null)
    {
        InitializeComponent();
        var isNew = supplier == null;
        Title = UiText.L(isNew ? "WindowAddTrader" : "WindowEditTrader");
        HeaderTitleText.Text = Title;

        if (supplier != null)
        {
            NameText.Text = supplier.Name;
            PhoneText.Text = supplier.Phone ?? string.Empty;
            WorkerNameText.Text = supplier.WorkerName ?? string.Empty;
            WorkerPhoneText.Text = supplier.WorkerPhone ?? string.Empty;
            NotesText.Text = supplier.Notes ?? string.Empty;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameText.Text))
        {
            MessageBox.Show(this, UiText.L("MsgNameRequired"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            NameText.Focus();
            return;
        }

        if (SupplierName.Length > 120)
        {
            MessageBox.Show(this, UiText.L("MsgNameTooLong"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            NameText.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SupplierPhone) && SupplierPhone.Length > 25)
        {
            MessageBox.Show(this, UiText.L("MsgPhoneInvalid"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            PhoneText.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(WorkerPhone) && WorkerPhone.Length > 25)
        {
            MessageBox.Show(this, UiText.L("MsgWorkerPhoneInvalid"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            WorkerPhoneText.Focus();
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
