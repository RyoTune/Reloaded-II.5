namespace Reloaded.Mod.Launcher.Lib.Remix.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class SelectFileConfig
{
    public string Title { get; set; } = "Select File";

    public string? Filter { get; set; }

    public bool AllowMultiple { get; set; } = false;
}
