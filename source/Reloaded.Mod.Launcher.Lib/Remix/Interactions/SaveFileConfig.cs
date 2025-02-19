namespace Reloaded.Mod.Launcher.Lib.Remix.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class SaveFileConfig
{
    public string Title { get; set; } = "Save File";

    public string? Filter { get; set; }

    public bool OverwritePrompt { get; set; } = true;

    public string? FileName { get; set; }
}
