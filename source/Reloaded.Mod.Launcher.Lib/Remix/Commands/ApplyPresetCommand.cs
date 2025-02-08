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
        if (preset != null && _modsVm.AllMods != null)
        {
            foreach (var mod in _modsVm.AllMods)
            {
                if (mod.IsEditable)
                {
                    mod.Enabled = preset.Mods.Any(x => x.Id == mod.Tuple.Config.ModId);
                }
            }
        }
    }
}
