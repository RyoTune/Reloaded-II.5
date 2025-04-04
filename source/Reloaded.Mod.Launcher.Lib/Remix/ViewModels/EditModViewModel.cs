using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Extensions;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using RemixToolkit.HostMod.Installer;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class EditModViewModel : ReactiveViewModelBase
{

    private readonly string DEFAULT_AUTO_ID = Random.Shared.Next(0, 1000000).ToString("000000");

    private readonly ModConfig _config;
    private readonly ApplicationConfigService _appsService;
    private readonly ModConfigService _modsService;
    private readonly PathTuple<ModConfig>? _mod;

    private bool _useAutoId;
    private IObservable<PropertyValue<BooleanGenericTuple<IApplicationConfig>, bool>>? _appsEnabledObs;
    private IObservable<PropertyValue<BooleanGenericTuple<ModConfig>, bool>>? _depsEnabledObs;
    private IObservable<object>? _resolversObs;

    public EditModViewModel(ApplicationConfigService appsService, ModConfigService modsService, ModConfig? preset = null)
    {
        _config = preset ?? new();
        _appsService = appsService;
        _modsService = modsService;

        _useAutoId = true;
        IsCreating = true;

        LoadDependencies();
        this.WhenActivated(Activate);
    }

    public EditModViewModel(ApplicationConfigService appsService, ModConfigService modsService, PathTuple<ModConfig> mod)
    {
        _appsService = appsService;
        _modsService = modsService;
        _mod = mod;
        _config = mod.Config;
        IsToolkitInstalled = _config.ModDependencies.Contains(HostModInstaller.REMIX_MOD_ID);

        if (mod.Config.TryGetIconPath(mod.Path, out var iconPath)) IconPath = iconPath;

        LoadDependencies();
        this.WhenActivated(Activate);
    }

    private void LoadDependencies()
    {
        // Add known applications.
        var apps = _appsService.Items;
        foreach (var app in apps)
        {
            bool isAppEnabled = _config.SupportedAppId.Contains(app.Config.AppId, StringComparer.OrdinalIgnoreCase) == true;
            Applications.Add(new BooleanGenericTuple<IApplicationConfig>(isAppEnabled, app.Config));
        }

        var mods = _modsService.Items;
        foreach (var mod in mods)
        {
            bool isModEnabled = _config.ModDependencies.Contains(mod.Config.ModId, StringComparer.OrdinalIgnoreCase) == true;
            Mods.Add(new BooleanGenericTuple<ModConfig>(isModEnabled, mod.Config));

            // Add unknown applications from mods.
            foreach (var appId in mod.Config.SupportedAppId)
            {
                if (!Applications.Any(x => x.Generic.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase)))
                {
                    bool isAppEnabled = _config.SupportedAppId.Contains(appId, StringComparer.OrdinalIgnoreCase) == true;
                    Applications.Add(new BooleanGenericTuple<IApplicationConfig>(isAppEnabled, new UnknownApplicationConfig(appId)));
                }
            }
        }

        if (!Mods.Any(x => x.Generic.ModId == HostModInstaller.REMIX_MOD_ID))
        {
            Mods.Add(new(false, new() { ModId = HostModInstaller.REMIX_MOD_ID, ModName = HostModInstaller.REMIX_MOD_NAME }));
        }

        _appsEnabledObs = Observable.Merge(Applications.Select(x => x.WhenPropertyChanged(x => x.Enabled, false)));
        _depsEnabledObs = Observable.Merge(Mods.Select(x => x.WhenPropertyChanged(x => x.Enabled, false)));

        // Use a dummy tuple since there is no tuple for new mods.
        // None of the resolvers seem to *need* it, but the interface requires it and changing it
        // would be more work...
        var dummyTuple = new PathTuple<ModConfig>("./dummy.json", _config);

        // Build Update Configurations
        foreach (var resolver in PackageResolverFactory.All)
        {
            var result = ResolverFactoryConfiguration.TryCreate(resolver, dummyTuple);
            if (result != null)
                Updates.Add(result);
        }

        var resolversEnabledObs = Updates.Select(x => x.WhenPropertyChanged(y => y.IsEnabled, false)).Cast<IObservable<object>>();
        var resolversPropsObs = Updates.Select(x => x.Configuration).OfType<INotifyPropertyChanged>().Select(x => x.WhenAnyPropertyChanged()).Cast<IObservable<object>>();

        _resolversObs = Observable.Merge(resolversEnabledObs.Concat(resolversPropsObs));
    }

    private void Activate(CompositeDisposable disp)
    {
        ConfirmModCommand = ReactiveCommand.Create(ConfirmMod, this.WhenAnyValue(vm => vm.Name, vm => vm.Id).Select(_ => CanConfirmMod()))
            .DisposeWith(disp);

        // Save when config changes.
        _config.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(_ =>
            {
                _mod?.Save();
            })
            .DisposeWith(disp);

        // Save on resolver settings change.
        _resolversObs?.Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(_ =>
            {
                var dummyTuple = new PathTuple<ModConfig>("./dummy.json", _config);
                foreach (var update in Updates)
                {
                    if (update.IsEnabled)
                        update.Factory.SetConfiguration(dummyTuple, update.Configuration);
                    else
                        _config.PluginData.Remove(update.Factory.ResolverId);
                }

                _mod?.Save();
            })
            .DisposeWith(disp);

        // Set supported app IDs.
        _appsEnabledObs?.Subscribe(_ => _config.SupportedAppId = this.Applications.Where(x => x.Enabled).Select(x => x.Generic.AppId).ToArray())
            .DisposeWith(disp);

        // Set mod dependencies.
        _depsEnabledObs?.Subscribe(_ => _config.ModDependencies = this.Mods.Where(x => x.Enabled).Select(x => x.Generic.ModId).ToArray())
            .DisposeWith(disp);

        // Update Auto ID.
        this.WhenAnyValue(vm => vm.Name)
            .Subscribe(name =>
            {
                if (IsCreating && _useAutoId)
                {
                    Id = GetAutoId();
                }
            })
            .DisposeWith(disp);

        // Save on deactivation (window close, etc).
        Disposable.Create(() =>
        {
            if (_mod != null)
            {
                _mod.Save();
            }
        })
        .DisposeWith(disp);
    }

    public bool IsCreating { get; }

    public string Id
    {
        get => _config.ModId;
        set
        {
            // User attempting to set custom ID, disable auto ID.
            if (_useAutoId && value != GetAutoId())
            {
                _useAutoId = false;
            }

            this.RaiseAndSetIfChanged(_config.ModId, value, _config, (m, v) => m.ModId = v);
        }
    }

    public string Name
    {
        get => _config.ModName;
        set => this.RaiseAndSetIfChanged(_config.ModName, value, _config, (m, v) => m.ModName = v);
    }

    public string Author
    {
        get => _config.ModAuthor;
        set => _config.ModAuthor = value;
    }

    public string Description
    {
        get => _config.ModDescription;
        set => _config.ModDescription = value;
    }

    public string Version
    {
        get => _config.ModVersion;
        set => _config.ModVersion = value;
    }

    public string ProjectUrl
    {
        get => _config.ProjectUrl;
        set => _config.ProjectUrl = value;
    }

    public string CreatorUrl
    {
        get => _config.CreatorUrl;
        set => _config.CreatorUrl = value;
    }

    public bool IsLibrary
    {
        get => _config.IsLibrary;
        set => _config.IsLibrary = value;
    }

    public bool IsUniversalMod
    {
        get => _config.IsUniversalMod;
        set => _config.IsUniversalMod = value;
    }

    public bool IsSeparator
    {
        get => _config.IsSeparator;
        set => _config.IsSeparator = value;
    }

    public string AppsFilter { get; set; } = string.Empty;

    public string ModsFilter { get; set; } = string.Empty;

    public bool IsToolkitInstalled { get; set; }

    public ObservableCollection<BooleanGenericTuple<IApplicationConfig>> Applications { get; } = [];

    public ObservableCollection<BooleanGenericTuple<ModConfig>> Mods { get; } = [];

    public ObservableCollection<ResolverFactoryConfiguration> Updates { get; } = [];

    public ReactiveCommand<Unit, Unit>? ConfirmModCommand { get; private set; }

    private void ConfirmMod()
    {
        if (!IsCreating)
        {
            return;
        }

        var fileSafeId = IOEx.ForceValidFilePath(Id);

        _config.ReleaseMetadataFileName = $"{fileSafeId}.ReleaseMetadata.json";
        var modDirectory = Path.Join(IoC.Get<LoaderConfig>().GetModConfigDirectory(), fileSafeId);
        var filePath = Path.Join(modDirectory, ModConfig.ConfigFileName);
        IConfig<ModConfig>.ToPath(_config, filePath);

        var newMod = new PathTuple<ModConfig>(filePath, _config);
        if (IsToolkitInstalled)
        {
            HostModInstaller.Install(newMod);
        }

        // Copy selected icon, if any.
        if (string.IsNullOrEmpty(IconPath) || !File.Exists(IconPath)) return;

        string modIconPath = Path.Join(modDirectory, newMod.Config.ModIcon);

        try
        {
            File.Copy(IconPath, modIconPath, true);
        }
        catch (Exception) { }
    }

    private bool CanConfirmMod()
    {
        if (!IsCreating) return true;

        return HasUniqueId() && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Id);
    }

    public bool FilterApp(BooleanGenericTuple<IApplicationConfig> item)
    {
        if (AppsFilter.Length <= 0)
            return true;

        var appNameResult = item.Generic.AppName.Contains(AppsFilter, StringComparison.InvariantCultureIgnoreCase);
        var appIdResult = item.Generic.AppId.Contains(AppsFilter, StringComparison.InvariantCultureIgnoreCase);
        return appNameResult || appIdResult;
    }

    public bool FilterMod(BooleanGenericTuple<ModConfig> item)
    {
        if (item.Generic.IsSeparator) return false;

        if (ModsFilter.Length <= 0)
            return true;

        return item.Generic.ModName.Contains(ModsFilter, StringComparison.InvariantCultureIgnoreCase);
    }

    public string IconPath { get; set; } = string.Empty;

    [RelayCommand]
    private async Task SelectIcon()
    {
        var files = await CommonInteractions.SelectFile.Handle(new() { Title = "Select Mod Icon", Filter = "Images (*.png)|*.png" });
        if (files == null || files.Length < 1)
        {
            return;
        }

        var selectedFile = files[0];

        if (_mod != null)
        {
            _mod.Config.ModIcon = "Preview.png";
            _mod.Save();

            string modDirectory = Path.GetDirectoryName(_mod.Path)!;
            string iconPath = Path.Join(modDirectory, _mod.Config.ModIcon);

            try
            {
                File.Copy(selectedFile, iconPath, true);
                IconPath = iconPath;
            }
            catch (Exception) { }
        }
        else
        {
            IconPath = selectedFile;
        }

        this.RaisePropertyChanged(nameof(IconPath));
    }

    [RelayCommand]
    private void ToggleToolkitInstall()
    {
        if (IsToolkitInstalled)
        {
            Mods.First(x => x.Generic.ModId == HostModInstaller.REMIX_MOD_ID).Enabled = false;
            IsToolkitInstalled = false;

            if (_mod != null) HostModInstaller.Uninstall(_mod);
        }
        else
        {
            Mods.First(x => x.Generic.ModId == HostModInstaller.REMIX_MOD_ID).Enabled = true;
            IsToolkitInstalled = true;

            // ConfirmMod handles installation for new mods.
            if (_mod != null) HostModInstaller.Install(_mod);
        }
    }

    private bool HasUniqueId() => !_modsService.ItemsById.ContainsKey(Id);

    private string GetAutoId() => IOEx.ForceValidFilePath($"{Name[..Math.Min(Name.Length, 24)]}_{DEFAULT_AUTO_ID}".Replace(" ", string.Empty));
}
