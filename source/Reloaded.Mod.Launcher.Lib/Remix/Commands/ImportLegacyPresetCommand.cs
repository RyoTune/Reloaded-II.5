using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Loader.IO.Remix.Mods;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ImportLegacyPresetCommand : ICommand
{
    private readonly PathTuple<ApplicationConfig> _appConfig;
    private readonly IEnumerable<ModConfig> _mods;

    public ImportLegacyPresetCommand(PathTuple<ApplicationConfig> appConfig, IEnumerable<ModConfig> mods)
    {
        _appConfig = appConfig;
        _mods = mods;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public async void Execute(object? parameter)
    {
        var files = await CommonInteractions.SelectFile.Handle(new() { Title = "Select Preset", Filter = "JSON (*.json)|*.json" });
        if (files.Length < 1) return;

        var name = await CommonInteractions.PromptTextInput.Handle(new() { Title = "Import Preset", Description = "Enter a name for imported preset." });
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var modSet = ModSet.FromFile(files[0]);
            _appConfig.Config.Presets.Add(new() { Name = name, Mods = modSet.EnabledMods.Select(id => new BasicModEntry() { Id = id, Name = GetModName(id) }).ToArray() });
            _appConfig.Save();
        }
        catch (Exception) { }
    }

    private string GetModName(string modId)
    {
        var mod = _mods.FirstOrDefault(x => x.ModId == modId);
        return mod?.ModName ?? modId;
    }
}
