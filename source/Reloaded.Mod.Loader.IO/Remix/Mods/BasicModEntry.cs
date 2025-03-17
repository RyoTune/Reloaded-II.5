namespace Reloaded.Mod.Loader.IO.Remix.Mods;

public class BasicModEntry
{
    public BasicModEntry()
    {
    }

    public BasicModEntry(ModConfig mod, bool enabled)
    {
        Id = mod.ModId;
        Name = mod.ModName;
        Enabled = enabled;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public bool Enabled { get; set; } = true;
}
