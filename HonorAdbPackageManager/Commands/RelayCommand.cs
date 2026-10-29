using System.Windows;
using System.Windows.Input;

namespace HonorAdbPackageManager.Commands;

/// <summary>
/// 通用命令：支持同步与 async lambda。
/// 异常统一捕获弹窗，避免 async void 崩溃进程。
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter)
    {
        try
        {
            _execute(parameter);
        }
        catch (Exception ex)
        {
            App.Log.Error($"命令执行异常：{ex}");
            MessageBox.Show($"操作失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void RaiseCanExecuteChanged() =>
        CommandManager.InvalidateRequerySuggested();
}
