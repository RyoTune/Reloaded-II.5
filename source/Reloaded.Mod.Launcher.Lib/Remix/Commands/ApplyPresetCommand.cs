using Reloaded.Mod.Loader.IO.Remix.Mods;

namespace Reloaded.Mod.Launcher.Lib.Remix.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ApplyPresetCommand : ICommand
{
    private readonly ConfigureModsViewModel _modsVm;
    private readonly ModsPreset? _preset;

    public ApplyPresetCommand(ConfigureModsViewModel modsVm, ModsPreset? preset = null)
    {
        _modsVm = modsVm;
        _preset = preset;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => parameter as ModsPreset != null || _preset != null;

    public void Execute(object? parameter)
    {
        var preset = parameter as ModsPreset ?? _preset;
        if (preset == null || _modsVm.AllMods == null) return;


        /* Enable and organize mods according to selected preset. */

        // Duplicate the preset mods list and append disabled
        // entries for any missing mods. Could probably be clever using more LINQ but meh.
        var presetEntries = preset.Mods.ToList();
        var missingEntries = _modsVm.AllMods.Where(mod => !presetEntries.Any(x => x.Id == mod.Tuple.Config.ModId)).Select(x => new BasicModEntry(x.Tuple.Config, false));
        presetEntries.AddRange(missingEntries);

        _modsVm.ApplicationTuple.Config.SortedMods = presetEntries.Select(x => x.Id).ToArray();
        _modsVm.ApplicationTuple.Config.EnabledMods = presetEntries.Where(x => x.Enabled).Select(x => x.Id).ToArray();
        _modsVm.ApplicationTuple.Save();

        _modsVm.BuildModList();
    }
}
