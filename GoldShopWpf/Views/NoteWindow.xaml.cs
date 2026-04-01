using System.Windows;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.Views;

public partial class NoteWindow : Window
{
    public string ClientName => ClientNameText.Text.Trim();
    public string NoteContent => NoteContentText.Text.Trim();

    public NoteWindow(ClientNote? note = null)
    {
        InitializeComponent();
        var isNew = note == null;
        Title = UiText.L(isNew ? "WindowAddNote" : "WindowEditNote");
        HeaderTitleText.Text = Title;

        if (note != null)
        {
            ClientNameText.Text = note.ClientName;
            NoteContentText.Text = note.Content;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ClientName))
        {
            MessageBox.Show(this, UiText.L("MsgClientNameRequired"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            ClientNameText.Focus();
            return;
        }

        if (ClientName.Length > 120)
        {
            MessageBox.Show(this, UiText.L("MsgClientNameTooLong"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            ClientNameText.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(NoteContent))
        {
            MessageBox.Show(this, UiText.L("MsgNoteContentRequired"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            NoteContentText.Focus();
            return;
        }

        if (NoteContent.Length > 4000)
        {
            MessageBox.Show(this, UiText.L("MsgNoteTooLong"), UiText.L("TitleValidation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            NoteContentText.Focus();
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
