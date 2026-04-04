namespace GoldShopWpf.ViewModels;

public enum ToastKind
{
    Success,
    Warning,
    Error
}

public class ToastMessageViewModel : ViewModelBase
{
    public ToastMessageViewModel(string message, ToastKind kind)
    {
        Message = message;
        Kind = kind;
    }

    public string Message { get; }
    public ToastKind Kind { get; }
}
