using System.Windows.Input;

namespace Huskui.Avalonia.Models;

internal class DummyCommand : ICommand
{
    public static readonly DummyCommand Instance = new();
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) { }

    #pragma warning disable 67
    public event EventHandler? CanExecuteChanged;
    #pragma warning restore 67
}