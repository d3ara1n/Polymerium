using System;
using System.Windows.Input;

namespace Polymerium.App.Models;

public class DummyCommand : ICommand
{
    public static DummyCommand Instance { get; } = new();

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        // do nothing
    }

    public event EventHandler? CanExecuteChanged;
}