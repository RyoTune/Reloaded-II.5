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

        var installedIds = _modsVm.AllMods.Select(x => x.Tuple.Config.ModId).ToHashSet();

        // Create any missing separator entries first.
        // Since they're empty mods, there *should* be no harm in sharing them via presets.
        var missSeparators = preset.Mods.Where(x => x.IsSeparator).Where(x => !installedIds.Contains(x.Id));
        foreach (var newSeparator in missSeparators) CreateSeparator(newSeparator);

        /* Enable and organize mods according to selected preset. */

        // Duplicate the preset mods list and append disabled
        // entries for any missing mods. Could probably be clever using more LINQ but meh.
        var presetEntries = preset.Mods.ToList();
        var missingEntries = installedIds.Where(modId => !presetEntries.Any(x => x.Id == modId)).Select(x => new BasicModEntry() { Id = x, Enabled = false });

        presetEntries.AddRange(missingEntries);

        _modsVm.ApplicationTuple.Config.SortedMods = presetEntries.Select(x => x.Id).ToArray();
        _modsVm.ApplicationTuple.Config.EnabledMods = presetEntries.Where(x => x.Enabled).Select(x => x.Id).ToArray();
        _modsVm.ApplicationTuple.Save();

        _modsVm.BuildModList();
    }

    private void CreateSeparator(BasicModEntry entry)
    {
        if (!entry.IsSeparator) return;

        var mod = new ModConfig() { ModId = entry.Id, ModName = entry.Name, IsSeparator = true, SupportedAppId = [_modsVm.ApplicationTuple.Config.AppId] };
        var modDirectory = Path.Join(IoC.Get<LoaderConfig>().GetModConfigDirectory(), mod.ModId);
        var filePath = Path.Join(modDirectory, ModConfig.ConfigFileName);
        IConfig<ModConfig>.ToPath(mod, filePath);
    }
}
