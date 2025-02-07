using Window = System.Windows.Window;

namespace Reloaded.Mod.Launcher.Controls;

public class WindowCommands
{
    public static readonly ICommand CloseWindowCommand = new CloseWindowCommand();
    public static readonly ICommand MinWindowCommand = new MinWindowCommand();
    public static readonly ICommand MaxWindowCommand = new MaxWindowCommand();
}

public class CloseWindowCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is Window win)
        {
            win.Close();
        }
    }
}

public class MinWindowCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is Window win)
        {
            win.WindowState = WindowState.Minimized;
        }
    }
}

public class MaxWindowCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is Window win)
        {
            win.WindowState = win.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
