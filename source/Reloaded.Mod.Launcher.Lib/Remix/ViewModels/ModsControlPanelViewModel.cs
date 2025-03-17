using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Loader.IO.Remix.Mods;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class ModsControlPanelViewModel : ViewModelBase, IActivatableViewModel
{
    private static readonly string[] PRESET_NAMES =
    [
        "Shuba shuba shuba",
        "Piss hair",
        "BROTHER, MAY I HAVE SOME OATS",
        "Clearly you did something wrong",
        "Great vegetables",
    ];

    private static readonly ModsPreset NEW_PRESET_ENTRY = new()
    {
        Name = "New Preset...",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMods))]
    [NotifyCanExecuteChangedFor(nameof(DeletePresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyPresetCommand))]
    private ModsPreset? _selectedPreset;

    private readonly PathTuple<ApplicationConfig> _appConfig;
    private readonly ConfigureModsViewModel _modsVm;
    private readonly LoaderConfig _loaderConfig;

    public ModsControlPanelViewModel(ConfigureModsViewModel modsVm, LoaderConfig loaderConfig)
    {
        _appConfig = modsVm.ApplicationTuple;
        _modsVm = modsVm;
        _loaderConfig = loaderConfig;

        Presets = [NEW_PRESET_ENTRY, .._appConfig.Config.Presets];
        SelectedPreset = NEW_PRESET_ENTRY;

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.WhenPropertyChanged(vm => vm.ShortcutsEnabled, false)
                .Subscribe(_ => IConfig<LoaderConfig>.ToPath(_loaderConfig, Paths.LoaderConfigPath))
                .DisposeWith(disp);

            Presets.ToObservableChangeSet()
            .Subscribe(set =>
            {
                foreach (var change in set)
                {
                    if (change.Reason == ListChangeReason.Moved)
                    {
                        var currIndex = _appConfig.Config.Presets.IndexOf(change.Item.Current);
                        if (currIndex == -1)
                        {
                            continue;
                        }

                        var newIndex = change.Item.CurrentIndex - 1;

                        // If preset was placed above NEW_PRESET_ENTRY.
                        if (newIndex < 0 || currIndex == newIndex)
                        {
                            continue;
                        }

                        _appConfig.Config.Presets.Move(currIndex, newIndex);
                        _appConfig.Save();
                    }
                }
            })
            .DisposeWith(disp);
        });
    }

    public ViewModelActivator Activator { get; } = new();

    public ObservableCollection<ModsPreset> Presets { get; }

    public ApplicationConfig AppConfig => _appConfig.Config;

    public bool ShortcutsEnabled
    {
        get => _loaderConfig.PresetShortcutsEnabled;
        set => this.SetProperty(_loaderConfig.PresetShortcutsEnabled, value, _loaderConfig, (m, v) => m.PresetShortcutsEnabled = v);
    }

    public BasicModEntry[]? SelectedMods
        => SelectedPreset == NEW_PRESET_ENTRY ? GetPresetModEntries(_modsVm.AllMods) : SelectedPreset?.Mods;

    private bool CanUsePresetCommand => SelectedPreset != null && SelectedPreset != NEW_PRESET_ENTRY;

    [RelayCommand]
    private void SetModsEnableState(bool value)
    {
        if (_modsVm.AllMods == null)
            return;

        foreach (var mod in _modsVm.AllMods)
            if (mod.IsEditable)
                mod.Enabled = value;
    }

    [RelayCommand]
    private async Task SavePreset()
    {
        var currentMods = GetPresetModEntries(_modsVm.AllMods).ToArray() ?? [];

        if (SelectedPreset == NEW_PRESET_ENTRY)
        {
            var name = await CommonInteractions.PromptTextInput.Handle(new()
            {
                Title = "New Preset",
                Description = "Enter a name for the preset.",
                Text = Random.Shared.Next(10) == 0 ? PRESET_NAMES[Random.Shared.Next(PRESET_NAMES.Length)] : string.Empty,
            });

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var newPreset = new ModsPreset()
            {
                Name = name,
                Mods = currentMods,
            };

            _appConfig.Config.Presets.Add(newPreset);
            Presets.Add(newPreset);
        }
        else if (SelectedPreset != null)
        {
            SelectedPreset.Mods = currentMods;
            this.OnPropertyChanged(nameof(SelectedMods));
        }

        _appConfig.Save();
    }

    [RelayCommand(CanExecute = nameof(CanUsePresetCommand))]
    private void DeletePreset()
    {
        if (SelectedPreset == null) return;

        // Remove from config first, SelectedPreset will be null
        // after being removed from local list.
        _appConfig.Config.Presets.Remove(SelectedPreset);
        Presets.Remove(SelectedPreset);

        _appConfig.Save();
    }

    [RelayCommand(CanExecute = nameof(CanUsePresetCommand))]
    private void ApplyPreset()
    {
        if (SelectedPreset == null || _modsVm.AllMods == null) return;

        /* Enable and organize mods according to selected preset. */

        // Duplicate the preset mods list and append disabled
        // entries for any missing mods. Could probably be clever using more LINQ but meh.
        var presetEntries = SelectedPreset.Mods.ToList();
        var missingEntries = _modsVm.AllMods.Where(mod => !presetEntries.Any(x => x.Id == mod.Tuple.Config.ModId)).Select(x => new BasicModEntry(x.Tuple.Config, false));
        presetEntries.AddRange(missingEntries);

        _appConfig.Config.SortedMods = presetEntries.Select(x => x.Id).ToArray();
        _appConfig.Config.EnabledMods = presetEntries.Where(x => x.Enabled).Select(x => x.Id).ToArray();
        _appConfig.Save();

        _modsVm.BuildModList();
    }

    [RelayCommand]
    private async Task ImportPreset()
    {
        var files = await CommonInteractions.SelectFile.Handle(new() { Title = "Import Mod Set", Filter = "Mod Set (*.json)|*.json" });
        if (files.Length < 1) return;

        var name = await CommonInteractions.PromptTextInput.Handle(new() { Title = "Import Mod Set", Description = "Enter a name for the imported Mod Set." });
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var modSet = ModSet.FromFile(files[0]);
            var newPreset = new ModsPreset() { Name = name, Mods = modSet.EnabledMods.Select(id => new BasicModEntry() { Id = id, Name = GetModName(id) }).ToArray() };

            // Duplicate code with saving new preset.
            // Should probably be an observable on Presets that then saves presets to actual config.
            Presets.Add(newPreset);
            _appConfig.Config.Presets.Add(newPreset);
        }
        catch (Exception) { }
    }

    private string GetModName(string modId)
    {
        var mod = _modsVm.AllMods?.FirstOrDefault(x => x.Tuple.Config.ModId == modId);
        return mod?.Tuple.Config.ModName ?? modId;
    }

    private static BasicModEntry[] GetPresetModEntries(IEnumerable<ModEntry>? mods)
        => mods?.Select(x => new BasicModEntry(x.Tuple.Config, x.Enabled == true))
        .ToArray() ?? [];
}
