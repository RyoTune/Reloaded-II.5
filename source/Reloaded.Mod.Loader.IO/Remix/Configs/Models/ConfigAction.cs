namespace Reloaded.Mod.Loader.IO.Remix.Configs.Models;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ConfigAction
{
    public string If { get; set; } = null;

    public string Using { get; set; } = string.Empty;

    public string Run { get; set; } = string.Empty;

    public string[] With { get; set; } = [];
}
