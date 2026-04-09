using System.ComponentModel;
using System.Runtime.CompilerServices;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private readonly List<INotifiesCanExecuteChanged> _trackedCommands = [];
    private bool _isBusy;
    private bool _isBusyVisible;
    private string _busyMessage = string.Empty;
    private CancellationTokenSource? _busyVisibilityCts;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseTrackedCommandStates();
            }
        }
    }

    public string BusyMessage
    {
        get => _busyMessage;
        protected set => SetProperty(ref _busyMessage, value);
    }

    public bool IsBusyVisible
    {
        get => _isBusyVisible;
        private set => SetProperty(ref _isBusyVisible, value);
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

    protected T TrackCommand<T>(T command) where T : class, INotifiesCanExecuteChanged
    {
        _trackedCommands.Add(command);
        return command;
    }

    protected void RaiseTrackedCommandStates()
    {
        foreach (var command in _trackedCommands)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    protected void ObserveBackgroundTask(Task task, string context, bool showGenericError = false)
    {
        var scheduler = SynchronizationContext.Current == null
            ? TaskScheduler.Current
            : TaskScheduler.FromCurrentSynchronizationContext();

        task.ContinueWith(t =>
        {
            var exception = t.Exception?.GetBaseException();
            if (exception == null)
            {
                return;
            }

            FileLogService.LogError(context, exception);
            if (showGenericError)
            {
                ToastService.ShowError(UiText.L("MsgGenericError"));
            }
        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
    }

    protected async Task RunBusyAsync(string busyMessage, Func<Task> operation, string? errorToast = null, bool rethrow = false)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        BusyMessage = busyMessage;
        ScheduleBusyVisibility();

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
            _busyVisibilityCts?.Cancel();
            _busyVisibilityCts?.Dispose();
            _busyVisibilityCts = null;
            IsBusyVisible = false;
            BusyMessage = string.Empty;
            IsBusy = false;
        }
    }

    private void ScheduleBusyVisibility()
    {
        _busyVisibilityCts?.Cancel();
        _busyVisibilityCts?.Dispose();
        _busyVisibilityCts = new CancellationTokenSource();
        var token = _busyVisibilityCts.Token;

        ObserveBackgroundTask(ShowBusyIndicatorAsync(token), "ViewModelBase.BusyVisibility");
    }

    private async Task ShowBusyIndicatorAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && IsBusy)
            {
                IsBusyVisible = true;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
