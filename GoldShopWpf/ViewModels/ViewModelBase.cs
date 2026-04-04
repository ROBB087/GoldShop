using System.ComponentModel;
using System.Runtime.CompilerServices;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _busyMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        protected set => SetProperty(ref _busyMessage, value);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected async Task RunBusyAsync(string busyMessage, Func<Task> operation, string? errorToast = null, bool rethrow = false)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        BusyMessage = busyMessage;

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            FileLogService.LogError("UI operation failed", ex);
            if (!string.IsNullOrWhiteSpace(errorToast))
            {
                ToastService.ShowError(errorToast);
            }
            else
            {
                ToastService.ShowError(UiText.L("MsgGenericError"));
            }

            if (rethrow)
            {
                throw;
            }
        }
        finally
        {
            BusyMessage = string.Empty;
            IsBusy = false;
        }
    }
}
