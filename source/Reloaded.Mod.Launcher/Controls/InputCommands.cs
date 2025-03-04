using Window = System.Windows.Window;

namespace Reloaded.Mod.Launcher.Controls;

internal class InputCommands
{
    public static readonly ConfirmInputCommand ConfirmInputCommand = new();
}

public class ConfirmInputCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is Window win)
        {
            win.DialogResult = true;
            win.Close();
        }
    }
}
