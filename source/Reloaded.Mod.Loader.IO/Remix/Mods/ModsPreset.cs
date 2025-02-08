using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace Reloaded.Mod.Loader.IO.Remix.Mods;

public partial class ModsPreset : ObservableObject
{
    private string _name;
    private BasicModEntry[] _mods;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public BasicModEntry[] Mods
    {
        get => _mods;
        set => SetProperty(ref _mods, value);
    }
}
