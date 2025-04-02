using System.Windows.Input;

namespace Huskui.Avalonia.Models;

internal class DummyCommand : ICommand
{
    public static readonly DummyCommand Instance = new();
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) { }
    public event EventHandler? CanExecuteChanged;
}