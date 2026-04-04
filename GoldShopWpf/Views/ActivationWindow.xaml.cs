using System.Windows;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class ActivationWindow : Window
{
    public ActivationWindow()
    {
        InitializeComponent();
        MachineIdTextBox.Text = LicenseService.GetMachineId();
    }

    private void OnCopyMachineId(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(MachineIdTextBox.Text);
        MessageBox.Show("تم نسخ معرف الجهاز.", "التفعيل", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnActivate(object sender, RoutedEventArgs e)
    {
        if (LicenseService.TryActivate(LicenseKeyTextBox.Text, out var error))
        {
            MessageBox.Show($"تم تفعيل البرنامج بنجاح.\nLicensed to: {LicenseService.LicensedTo}", "التفعيل", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show(error, "التفعيل", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
