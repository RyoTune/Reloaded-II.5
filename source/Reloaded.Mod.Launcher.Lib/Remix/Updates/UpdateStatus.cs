namespace Reloaded.Mod.Launcher.Lib.Remix.Updates;

/// <summary>
/// Mod update status.
/// </summary>
public enum UpdateStatus
{
    /// <summary>
    /// No method for updating is available.
    /// </summary>
    None,

    /// <summary>
    /// Supports some method for updating.
    /// </summary>
    Supported,

    /// <summary>
    /// An update is available and pending install.
    /// </summary>
    Pending,

    /// <summary>
    /// Currently checking for an update.
    /// </summary>
    Checking,
}
