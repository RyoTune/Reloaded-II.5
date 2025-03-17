using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Commands;
using Reloaded.Mod.Launcher.Lib.Remix.Extensions;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using Reloaded.Mod.Loader.IO.Remix.Mods;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Models.ViewModel.Application;

/// <summary>
/// ViewModel allowing for the configuration of mods to be loaded for a certain game.
/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ConfigureModsViewModel : ReactiveViewModelBase
{
    /// <summary>
    /// Special tag that includes all items.
    /// </summary>
    public const string IncludeAllTag = "! ALL TAGS !";

    /// <summary>
    /// Special tag that includes items that don't have custom code.
    /// </summary>
    public const string CodeInjectionTag = "Code Injection";

    /// <summary>
    /// Special tag that includes items that have custom code.
    /// </summary>
    public const string NoCodeInjectionTag = "No Code Injection";

    /// <summary>
    /// Special tag that includes items that use native code.
    /// </summary>
    public const string NativeModTag = "Native Mod";

    /// <summary>
    /// Special tag that excludes universal mods.
    /// </summary>
    public const string NoUniversalModsTag = "No Universal Mods";

    /// <summary>
    /// All mods available for the game.
    /// </summary>
    public ObservableCollection<ModEntry>? AllMods { get; set; } = null!;

    /// <summary>
    /// The currently highlighted mod.
    /// </summary>
    public ModEntry? SelectedMod { get; set; }

    /// <summary>
    /// Stores the currently selected application.
    /// </summary>
    public PathTuple<ApplicationConfig> ApplicationTuple { get; set; }

    /// <summary>
    /// List of all selectable tags to filter by.
    /// </summary>
    public ObservableCollection<string> AllTags { get; set; } = new ObservableCollection<string>();

    /// <summary/>
    public string SelectedTag { get; set; } = string.Empty;

    /// <summary/>
    public OpenModFolderCommand OpenModFolderCommand { get; set; } = null!;

    /// <summary/>
    public ConfigureModCommand ConfigureModCommand { get; set; } = null!;

    /// <summary/>
    public VisitModProjectUrlCommand VisitModProjectUrlCommand { get; set; } = null!;

    /// <summary/>
    public DeleteModCommand DeleteModCommand { get; set; } = null!;

    /// <summary/>
    public EditModCommand EditModCommand { get; set; } = null!;

    /// <summary/>
    public EditModUserConfigCommand EditModUserConfigCommand { get; set; } = null!;

    /// <summary/>
    public PublishModCommand PublishModCommand { get; set; } = null!;

    /// <summary/>
    public OpenUserConfigFolderCommand OpenUserConfigFolderCommand { get; set; } = null!;

    /// <summary/>
    public bool IsCompact
    {
        get => _loaderConfig.ModsList_IsCompact;
        set => this.RaiseAndSetIfChanged(_loaderConfig.ModsList_IsCompact, value, _loaderConfig, (m, newValue) => m.ModsList_IsCompact = newValue);
    }

    /// <summary/>
    public bool IsHorizontal
    {
        get => _loaderConfig.ModsList_IsHorizontal;
        set => this.RaiseAndSetIfChanged(_loaderConfig.ModsList_IsHorizontal, value, _loaderConfig, (m, newValue) => m.ModsList_IsHorizontal = newValue);
    }

    /// <summary/>
    public bool ShowHidden
    {
        get => _loaderConfig.ShowHiddenMods;
        set => this.RaiseAndSetIfChanged(_loaderConfig.ShowHiddenMods, value, _loaderConfig, (m, newValue) => m.ShowHiddenMods = newValue);
    }

    /// <summary/>
    public ModsControlPanelViewModel ModsControlPanelVm { get; }

    private ModEntry? _cachedModEntry;
    private ApplicationViewModel _applicationViewModel;
    private readonly ModUserConfigService _userConfigService;
    private CancellationTokenSource _saveToken;
    private IDisposable? _saveStream;
    private readonly LoaderConfig _loaderConfig;

    /// <inheritdoc />
    public ConfigureModsViewModel(ApplicationViewModel model, ModUserConfigService userConfigService, LoaderConfig loaderConfig)
    {
        ApplicationTuple = model.ApplicationTuple;
        _applicationViewModel = model;
        _userConfigService = userConfigService;
        _saveToken = new CancellationTokenSource();
        _loaderConfig = loaderConfig;

        // Wait for parent to fully initialize.
        _applicationViewModel.OnGetModsForThisApp += BuildModList;
        _applicationViewModel.OnLoadModSet += BuildModList;
        BuildModList();

        SelectedMod = AllMods.FirstOrDefault(x => x.IsHidden == false);
        PropertyChanged += OnSelectedModChanged;
        UpdateCommands();

        ModsControlPanelVm = new(this, loaderConfig);

        ToggleModHideCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedMod == null) return;
            SelectedMod.IsHidden = !SelectedMod.IsHidden;
        });

        _showShortcuts = _loaderConfig.WhenValueChanged(x => x.PresetShortcutsEnabled).ToProperty(this, vm => vm.ShowShortcuts);
        _shortcuts = ApplicationTuple.Config.Presets.ToObservableChangeSet().AutoRefresh()
            .Select(x =>
            {
                var shortcuts = new List<PresetShortcut>();
                for (int i = 0; i < ApplicationTuple.Config.Presets.Count && i < 9; i++)
                {
                    shortcuts.Add(new(i + 1, ApplicationTuple.Config.Presets[i], new(this, ApplicationTuple.Config.Presets[i])));
                }

                return shortcuts.ToArray();
            })
            .ToProperty(this, vm => vm.Shortcuts);

        this.WhenActivated((CompositeDisposable disp) =>
        {
            _showShortcuts.DisposeWith(disp);
            _shortcuts.DisposeWith(disp);

            // Save Loader config when its settings are changed.
            this.WhenAnyValue(vm => vm.ShowHidden, vm => vm.IsHorizontal, vm => vm.IsCompact)
            .Subscribe(_ => IConfig<LoaderConfig>.ToPath(_loaderConfig, Paths.LoaderConfigPath))
            .DisposeWith(disp);

            // Save hidden mods settings.
            this.ToggleModHideCommand.Subscribe(_ =>
            {
                _loaderConfig.HiddenModsIds = AllMods.Where(x => x.IsHidden).Select(x => x.Tuple.Config.ModId).ToArray();
                IConfig<LoaderConfig>.ToPath(_loaderConfig, Paths.LoaderConfigPath);
            });

            // Update number of mods hidden.
            Observable.Merge(this.WhenPropertyChanged(vm => vm.AllMods), this.ToggleModHideCommand.Select(x => (object)x))
            .Subscribe(_ => NumHiddenMods = AllMods?.Where(x => x.IsHidden).Count() ?? 0)
            .DisposeWith(disp);

            Disposable.Create(() =>
            {
                _applicationViewModel.OnLoadModSet -= BuildModList;
                _applicationViewModel.OnGetModsForThisApp -= BuildModList;
                _saveToken?.Dispose();
                _saveStream?.Dispose();
            })
            .DisposeWith(disp);
        });
    }

    // Remix
    private readonly ObservableAsPropertyHelper<bool> _showShortcuts;
    public bool ShowShortcuts => _showShortcuts.Value;

    public record PresetShortcut(int Id, ModsPreset Preset, ApplyPresetCommand ApplyPresetCommand);

    private readonly ObservableAsPropertyHelper<PresetShortcut[]> _shortcuts;
    public PresetShortcut[] Shortcuts => _shortcuts.Value;

    public ReactiveCommand<Unit, Unit> ToggleModHideCommand { get; }

    public int NumHiddenMods { get; set; } = 0;

    /// <summary>
    /// Builds the list of mods displayed to the user.
    /// </summary>
    public void BuildModList()
    {
        if (_saveStream != null)
        {
            _saveStream.Dispose();
        }

        var modsForThisApp = _applicationViewModel.ModsForThisApp.ToArray();
        AllMods = new ObservableCollection<ModEntry>(GetInitialModSet(modsForThisApp, ApplicationTuple));

        var enableObs = AllMods.Select(x => x.WhenPropertyChanged(m => m.Enabled)).Merge();
        var modsChangedObs = AllMods.ToObservableChangeSet().AutoRefresh();

        _saveStream = Observable.Merge<object>(enableObs, modsChangedObs).Throttle(TimeSpan.FromMilliseconds(250)).Skip(1).Subscribe(async _ => await SaveApp());

        Collections.ModifyObservableCollection(AllTags, GetTags(modsForThisApp).OrderBy(x => x));
        if (string.IsNullOrEmpty(SelectedTag))
            SelectedTag = AllTags[0];
    }

    /// <summary>
    /// Builds the initial set of mods to display in the list.
    /// </summary>
    private List<ModEntry> GetInitialModSet(PathTuple<ModConfig>[] modsForThisApp, PathTuple<ApplicationConfig> applicationTuple)
    {
        // Get dictionary of mods for this app by Mod ID
        var modDictionary = new Dictionary<string, PathTuple<ModConfig>>();
        foreach (var mod in modsForThisApp)
            modDictionary[mod.Config.ModId] = mod;

        var totalModList = new List<ModEntry>(modsForThisApp.Length);

        if (applicationTuple.Config.PreserveDisabledModOrder)
        {
            // Modern Behaviour: Mod Order is Preserved
            var enabledModIds = applicationTuple.Config.EnabledMods.Where(modDictionary.ContainsKey).Distinct().ToArray();
            var sortedModIds = applicationTuple.Config.SortedMods.Where(modDictionary.ContainsKey).Distinct().ToArray();

            var enabledModIdSet = enabledModIds.ToHashSet();
            var sortedModIdSet = sortedModIds.ToHashSet();

            // Add sorted mods.
            foreach (var sortedModId in sortedModIds)
                totalModList.Add(MakeModEntry(enabledModIdSet.Contains(sortedModId), modDictionary[sortedModId]));

            // Add enabled mods that were not in the sorted mod collection.
            // This can happen in case of config upgrade from an older version.
            foreach (var enabledModId in enabledModIds.Where(x => !sortedModIdSet.Contains(x)))
                totalModList.Add(MakeModEntry(true, modDictionary[enabledModId]));

            // Add the remaining mods on the bottom of the list as disabled.
            var remainingMods = modsForThisApp.Where(x => !enabledModIdSet.Contains(x.Config.ModId) && !sortedModIdSet.Contains(x.Config.ModId)).OrderBy(x => x.Config.ModName);
            totalModList.AddRange(remainingMods.Select(x => MakeModEntry(false, x)));
        }
        else
        {
            // Classic Behaviour: Disabled Mods are Alphabetical by Name
            var enabledModIds = applicationTuple.Config.EnabledMods;

            // Add enabled mods.
            foreach (var enabledModId in enabledModIds)
            {
                if (modDictionary.ContainsKey(enabledModId))
                    totalModList.Add(MakeModEntry(true, modDictionary[enabledModId]));
            }

            // Add disabled mods.
            var enabledModIdSet = enabledModIds.ToHashSet();
            var disabledMods = modsForThisApp.Where(x => !enabledModIdSet.Contains(x.Config.ModId)).OrderBy(x => x.Config.ModName);
            totalModList.AddRange(disabledMods.Select(x => MakeModEntry(false, x)));
        }

        return totalModList;
    }

    /// <summary>
    /// Builds the initial set of mods to display in the list.
    /// </summary>
    private HashSet<string> GetTags(PathTuple<ModConfig>[] modsForThisApp)
    {
        // Note: Must put items in top to bottom load order.
        var tags = new HashSet<string>();
        tags.Add(IncludeAllTag);

        foreach (var mod in modsForThisApp)
        {
            foreach (var tag in mod.Config.Tags)
                tags.Add(tag);

            // Auto-tags
            if (mod.Config.HasDllPath())
                tags.Add(CodeInjectionTag);
            else
                tags.Add(NoCodeInjectionTag);

            if (mod.Config.IsUniversalMod)
                tags.Add(NoUniversalModsTag);

            if (mod.Config.IsNativeMod(""))
                tags.Add(NativeModTag);
        }
        return tags;
    }

    private ModEntry MakeModEntry(bool? isEnabled, PathTuple<ModConfig> item)
    {
        // Make BooleanGenericTuple that saves application on Enabled change.
        var userConfig = _userConfigService.ItemsById.GetValueOrDefault(item.Config.ModId);
        var isHidden = _loaderConfig.HiddenModsIds.Contains(item.Config.ModId);
        var tuple = new ModEntry(isEnabled, isHidden, item, new(item, userConfig, ApplicationTuple));
        return tuple;
    }

    // == Events ==

    private async Task SaveApp()
    {
        _saveToken.Cancel();
        _saveToken = new CancellationTokenSource();

        try
        {
            // Don't update this if user doesn't want to preserve their order, in
            // case the user wants to backtrack and revert. 'e.g. I want to 'try' the other option'.
            if (ApplicationTuple.Config.PreserveDisabledModOrder)
                ApplicationTuple.Config.SortedMods = AllMods!.Select(x => x.Tuple.Config.ModId).ToArray();

            ApplicationTuple.Config.EnabledMods = AllMods!.Where(x => x.Enabled == true).Select(x => x.Tuple.Config.ModId).ToArray();
            await ApplicationTuple.SaveAsync(_saveToken.Token);
        }
        catch (TaskCanceledException) { /* Ignored */ }
    }

    [SuppressPropertyChangedWarnings]
    private void OnSelectedModChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedMod))
            UpdateCommands();
    }

    private void UpdateCommands()
    {
        // Some operations like swapping order of 2 lists might fire a 
        // event with SelectedMod == null, then select our mod again.
        // Setting up some commands (particularly ConfigureMod) can cause lag, so let's mitigate this.
        if (SelectedMod == null || SelectedMod == _cachedModEntry) 
            return;

        OpenModFolderCommand = new OpenModFolderCommand(SelectedMod.Tuple);
        EditModCommand = new EditModCommand(SelectedMod.Tuple, null);
        PublishModCommand = new PublishModCommand(SelectedMod.Tuple);
        VisitModProjectUrlCommand = new VisitModProjectUrlCommand(SelectedMod.Tuple);
        DeleteModCommand = new DeleteModCommand(SelectedMod.Tuple);

        var userConfig = _userConfigService.ItemsById.GetValueOrDefault(SelectedMod.Tuple.Config.ModId);
        EditModUserConfigCommand = new EditModUserConfigCommand(userConfig);
        OpenUserConfigFolderCommand = new OpenUserConfigFolderCommand(userConfig);
        ConfigureModCommand = new ConfigureModCommand(SelectedMod.Tuple, userConfig, ApplicationTuple);
        _cachedModEntry = SelectedMod;
    }
}