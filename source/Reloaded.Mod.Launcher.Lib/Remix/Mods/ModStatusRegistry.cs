namespace Reloaded.Mod.Launcher.Lib.Remix.Mods;

/// <summary>
/// Manages the status of mods.
/// Re-uses mod statuses to fix performance issues and not break existing bindings.
/// </summary>
public static class ModStatusRegistry
{
    private static readonly Dictionary<string, ModStatus> _status = [];

    /// <summary/>
    public static ModStatus GetModStatus(PathTuple<ModConfig> tuple)
    {
        if (_status.TryGetValue(tuple.Config.ModId, out ModStatus? status))
        {
            status.Refresh(tuple);
            return status;
        }

        var newStatus = new ModStatus(tuple);
        _status[tuple.Config.ModId] = newStatus;

        return newStatus;
    }

    /// <summary>
    /// Refreshes all mod statuses.
    /// </summary>
    public static void RefreshAll()
    {
        foreach (var item in _status) item.Value.Refresh();
    }
}
