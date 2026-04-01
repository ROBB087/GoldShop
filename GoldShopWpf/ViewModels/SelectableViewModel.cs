namespace GoldShopWpf.ViewModels;

public abstract class SelectableViewModel : ViewModelBase
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
