namespace Reloaded.Mod.Loader.IO.Remix.Configs.Models;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ConfigAction
{
    /// <summary>
    /// (Optional) Condition that determines if the action should run.
    /// </summary>
    public string If { get; set; } = null;

    /// <summary>
    /// Full name of API to use.
    /// </summary>
    public string Using { get; set; } = string.Empty;

    /// <summary>
    /// Name of function in API to run.
    /// </summary>
    public string Run { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to run function with.
    /// </summary>
    public string[] With { get; set; } = [];

    /// <summary>
    /// (Optional) Variable name to store function return value as.
    /// </summary>
    public string Output { get; set; } = null;
}
