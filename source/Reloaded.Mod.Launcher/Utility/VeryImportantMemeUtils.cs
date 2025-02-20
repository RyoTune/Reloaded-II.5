namespace Reloaded.Mod.Launcher.Utility;

public static class VeryImportantMemeUtils
{
    private static bool _hasChecked;
    private static string? _rinFile;

    public static bool TryRinFile([NotNullWhen(true)] out string? rinFile)
    {
        if (_hasChecked)
        {
            rinFile = _rinFile;
            return rinFile != null;
        }

        _rinFile = Path.Join(Path.GetDirectoryName(Lib.IoC.Get<LoaderConfig>().ApplicationConfigDirectory), "rin.file");
        if (!File.Exists(_rinFile)) _rinFile = null;

        _hasChecked = true;
        rinFile = _rinFile;

        return rinFile != null;
    }
}
