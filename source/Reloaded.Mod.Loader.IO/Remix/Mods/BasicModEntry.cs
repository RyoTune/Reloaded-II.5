namespace Reloaded.Mod.Loader.IO.Remix.Mods;

public class BasicModEntry
{
    public BasicModEntry()
    {
    }

    public BasicModEntry(ModConfig mod)
    {
        Id = mod.ModId;
        Name = mod.ModName;
    }

    public string Id { get; set; }

    public string Name { get; set; }
}
