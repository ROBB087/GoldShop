using GoldShopCore.Models;

namespace GoldShopWpf.ViewModels;

public class NotesGridItem : ViewModelBase
{
    private bool _isSelected;

    public int Id { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ClientNote ToClientNote() => new()
    {
        Id = Id,
        ClientName = ClientName,
        Content = Content,
        CreatedAt = CreatedAt
    };
}
