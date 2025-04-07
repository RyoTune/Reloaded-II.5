using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.Extensions;

internal static class ApplicationConfigExtensions
{
    /// <summary>
    /// Should match equivalent function in <see cref="EditAppViewModel"/>.
    /// </summary>
    /// <param name="appTuple">App tuple.</param>
    /// <returns>Whether a new application path was successfully selected.</returns>
    public static bool SelectAppPath(this PathTuple<ApplicationConfig> appTuple)
    {
        var newAppPath = CommonInteractions.SaveFile.Handle(new()
        {
            Title = "Select Application",
            Filter = "Application (*.exe)|*.exe",
            OverwritePrompt = false,
        }).Wait();

        if (string.IsNullOrEmpty(newAppPath))
        {
            return false;
        }

        // Resolve SymLink.
        // Original warns if trying to select a *new* application in EditApp
        // instead of AddApp. That's on them yall...
        var fileInfo = new FileInfo(newAppPath);
        if (fileInfo.LinkTarget != null) { newAppPath = fileInfo.LinkTarget; }

        appTuple.Config.AppLocation = newAppPath;
        appTuple.Config.WorkingDirectory = Path.GetDirectoryName(newAppPath)!;

        // Handle MsStore stuff
        var isMsStore = TryUnprotectGamePassGame.TryIt(newAppPath);
        if (isMsStore) { appTuple.Config.ReloadedMode = ReloadedMode.External; }

        appTuple.Save();
        return true;
    }
}
