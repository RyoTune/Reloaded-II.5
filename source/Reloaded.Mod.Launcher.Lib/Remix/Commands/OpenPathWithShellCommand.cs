
namespace Reloaded.Mod.Launcher.Lib.Remix.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class OpenPathWithShellCommand : ICommand
{
    public static readonly OpenPathWithShellCommand Instance = new();

    private readonly string? _path;

    public event EventHandler? CanExecuteChanged;

    public OpenPathWithShellCommand()
    {
    }

    public OpenPathWithShellCommand(string? path)
    {
        _path = path;
    }

    public bool CanExecute(object? parameter)
    {
        if (_path != null && (Directory.Exists(_path) || File.Exists(_path) || IsWebPath(_path)))
        {
            return true;
        }

        if (parameter is string paramPath)
        {
            if (Directory.Exists(paramPath) || File.Exists(paramPath) || IsWebPath(paramPath))
            {
                return true;
            }
        }

        return false;
    }

    public void Execute(object? parameter)
    {
        if (_path != null)
        {
            Process.Start(new ProcessStartInfo() { FileName = _path, UseShellExecute = true });
        }

        if (parameter is string paramPath)
        {
            Process.Start(new ProcessStartInfo() { FileName = paramPath, UseShellExecute = true });
        }
    }

    private static bool IsWebPath(string path) => path.StartsWith("https://") || path.StartsWith("http://");
}
