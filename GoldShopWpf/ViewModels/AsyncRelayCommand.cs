using System.Windows.Input;
using GoldShopCore.Services;
using GoldShopWpf.Services;

namespace GoldShopWpf.ViewModels;

public class AsyncRelayCommand : ICommand, INotifiesCanExecuteChanged
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value)
            {
                return;
            }

            _isExecuting = value;
            RaiseCanExecuteChanged();
        }
    }

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _ = ExecuteAsync(parameter);
    }

    private async Task ExecuteAsync(object? parameter)
    {
        IsExecuting = true;
        try
        {
            await _executeAsync(parameter);
        }
        catch (Exception ex)
        {
            FileLogService.LogError("Async command execution failed", ex);
            ExceptionReporter.Report(ex, "Async command execution failed");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
