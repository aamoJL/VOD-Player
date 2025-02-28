using System;
using System.Windows.Input;

namespace VODPlayer;

public partial class RelayCommand(Action? execute, Func<bool>? canExecute = null) : ICommand
{
  protected Action? _execute = execute;
  protected Func<bool>? _canExecute = canExecute;

  public event EventHandler? CanExecuteChanged;

  public bool CanExecute(object? parameter)
    => _canExecute?.Invoke() ?? true;

  public void Execute(object? parameter)
  {
    if (CanExecute(parameter))
      _execute?.Invoke();
  }

  public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
