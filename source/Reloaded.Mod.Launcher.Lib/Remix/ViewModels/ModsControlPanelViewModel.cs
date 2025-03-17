using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Commands;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Loader.IO.Remix.Mods;
using Reloaded.Mod.Loader.IO.Remix.Serializers;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class ModsControlPanelViewModel : ViewModelBase, IActivatableViewModel
{
    private const string PRESET_EXT = ".rxpreset";

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
    [NotifyCanExecuteChangedFor(nameof(ExportPresetCommand))]
    private ModsPreset? _selectedPreset;

    private readonly PathTuple<ApplicationConfig> _appConfig;
    private readonly ConfigureModsViewModel _modsVm;
    private readonly LoaderConfig _loaderConfig;
    private readonly ApplyPresetCommand _applyPreset;

    public ModsControlPanelViewModel(ConfigureModsViewModel modsVm, LoaderConfig loaderConfig)
    {
        _appConfig = modsVm.ApplicationTuple;
        _modsVm = modsVm;
        _loaderConfig = loaderConfig;
        _applyPreset = new ApplyPresetCommand(modsVm);

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
    private void ApplyPreset() => _applyPreset.Execute(SelectedPreset);

    [RelayCommand]
    private async Task ImportPreset()
    {
        var files = await CommonInteractions.SelectFile.Handle(new() { Title = "Import Preset", Filter = $"Preset (*{PRESET_EXT};*.json)|*{PRESET_EXT};*.json" });
        if (files.Length < 1) return;

        var selectedFile = files[0];
        var fileExt = Path.GetExtension(selectedFile);

        ModsPreset? newPreset = null;

        try
        {
            // Load Remix preset.
            if (fileExt == PRESET_EXT)
            {
                newPreset = YamlSerializer.DeserializeFile<ModsPreset>(selectedFile);
            }

            // Convert Reloaded ModSet to Remix preset.
            else
            {
                var modSet = ModSet.FromFile(files[0]);
                newPreset = new ModsPreset() { Name = string.Empty, Mods = modSet.EnabledMods.Select(id => new BasicModEntry() { Id = id, Name = GetModName(id) }).ToArray() };
            }
        }
        catch (Exception)
        {
            return;
        }


        var finalName = await CommonInteractions.PromptTextInput.Handle(new() { Title = "Import Preset", Description = "Enter a name for the preset.", Text = newPreset.Name });
        if (string.IsNullOrEmpty(finalName)) return;

        newPreset.Name = finalName;

        Presets.Add(newPreset);
        _appConfig.Config.Presets.Add(newPreset);
    }

    [RelayCommand(CanExecute = nameof(CanUsePresetCommand))]
    private async Task ExportPreset()
    {
        var defaultName = $"{Path.GetFileNameWithoutExtension(_appConfig.Config.AppId)} - {SelectedPreset!.Name}{PRESET_EXT}";
        var outputFile = await CommonInteractions.SaveFile.Handle(new() { Title = "Export Preset", FileName = defaultName, Filter = $"Preset (*{PRESET_EXT})|*{PRESET_EXT}" });
        if (string.IsNullOrEmpty(outputFile)) return;

        try
        {
            YamlSerializer.SerializeFile(outputFile, SelectedPreset);
        } catch (Exception) { }
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
