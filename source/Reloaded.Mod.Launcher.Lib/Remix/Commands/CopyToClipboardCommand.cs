
using System.Windows;

namespace Reloaded.Mod.Launcher.Lib.Remix.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class CopyToClipboardCommand : ICommand
{
    public static readonly CopyToClipboardCommand Instance = new();

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is string text)
        {
            Clipboard.SetText(text);
        }
    }
}
