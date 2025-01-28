using Reloaded.Mod.Launcher.Lib.Remix.Mods;

namespace Reloaded.Mod.Launcher.Lib.Remix.Updates;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class UpdateService
{
    private static ModUpdateSummary? _summary;
    private static Updater? _updater;

    public static bool IsChecking { get; private set; }

    public static void OpenUpdater()
    {
        if (_updater != null && _summary?.HasUpdates() == true)
        {
            Actions.ShowModUpdateDialog.Invoke(new ModUpdateDialogViewModel(_updater, _summary));
        }
    }

    public static async Task CheckForUpdates()
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;
        ModStatusRegistry.RefreshAll();

        var (summary, updater) = await Update.GetUpdateData();
        _summary = summary;
        _updater = updater;

        IsChecking = false;
        ModStatusRegistry.RefreshAll();
    }

    public static bool HasModUpdate(PathTuple<ModConfig> tuple)
    {
        // Get latest result from update check.
        var lastResult = _summary?.ManagerModResultPairs.FirstOrDefault(x => x.ModTuple.Config.ModId == tuple.Config.ModId)?.Result;

        if (lastResult != null && lastResult.LastVersion != null)
        {
            // Compare version from update result to current mod version.
            // Ideally, the updater would remove it if it was updated but it doesn't
            // and I don't feel like changing it...
            if (NuGetVersion.TryParse(tuple.Config.ModVersion, out var currVer)
                && currVer >= lastResult.LastVersion)
            {
                _summary?.RemoveByModId([tuple.Config.ModId]);
                return false;
            }

            return lastResult.CanUpdate;
        }

        return false;
    }
}
