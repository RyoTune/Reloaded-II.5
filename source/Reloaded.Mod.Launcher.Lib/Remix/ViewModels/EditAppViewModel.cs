using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Commands;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Launcher.Lib.Remix.Utils;
using System.Reactive;
using ReactiveUI.SourceGenerators;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Reloaded.Mod.Launcher.Lib.Remix.Extensions;
using Reloaded.Mod.Loader.IO.Remix.Apps;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class EditAppViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    [Reactive]
    private AppVersion? _selectedVersion;

    private readonly ApplicationConfig _appConfig;

    public EditAppViewModel(PathTuple<ApplicationConfig> appTuple)
    {
        _appConfig = appTuple.Config;

        AppTuple = appTuple;
        Versions = AppVersions.GetAvailableVersions(appTuple.Config);
        SelectedVersion = AppVersions.FindAppByVersion(_appConfig.TargetAppVersion, Versions);

        MakeShortcutCommand = new(appTuple, Lib.IconConverter);
        DeleteAppCommand = ReactiveCommand.Create(DeleteApp);

        // Build Package Provider Configurations
        foreach (var provider in PackageProviderFactory.All)
        {
            var result = ProviderFactoryConfiguration.TryCreate(provider, appTuple);
            if (result != null)
                PackageProviders.Add(result);
        }

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.WhenValueChanged(vm => vm.SelectedVersion)
            .Select(x => x?.Version)
            .Subscribe(version => _appConfig.TargetAppVersion = version?.ToString())
            .DisposeWith(disp);

            this.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(_ => AppTuple.Save())
            .DisposeWith(disp);

            this.WhenAnyValue(vm => vm.ReloadedMode, vm => vm.AppPath)
            .Select(x => x.Item1)
            .Subscribe(mode =>
            {
                switch (mode)
                {
                    case ReloadedMode.Default:
                        {
                            _appConfig.DontInject = false;
                            _appConfig.AutoInject = false;
                            if (!AsiLoader.TryRemoveAsi(AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to remove.
                            }
                        }
                        break;
                    case ReloadedMode.External:
                        {
                            _appConfig.DontInject = true;
                            _appConfig.AutoInject = false;
                            if (!AsiLoader.TryDeployAsi(AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to deploy.
                            }
                        }
                        break;
                    case ReloadedMode.AutoInject:
                        {
                            _appConfig.DontInject = true;
                            _appConfig.AutoInject = true;
                            if (!AsiLoader.TryRemoveAsi(AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to remove.
                            }
                        }
                        break;
                    case ReloadedMode.Disabled:
                    default:
                        {
                            _appConfig.DontInject = true;
                            _appConfig.AutoInject = false;
                            if (!AsiLoader.TryRemoveAsi(AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to remove.
                            }
                        }
                        break;
                }

                AppTuple?.Save();
            })
            .DisposeWith(disp);

            Disposable.Create(() =>
            {
                if (AppTuple == null) return;

                try
                {
                    // Save Plugins
                    foreach (var provider in PackageProviders)
                    {
                        if (provider.IsEnabled)
                            provider.Factory.SetConfiguration(AppTuple, provider.Configuration);
                        else
                            AppTuple.Config.PluginData.Remove(provider.Factory.ResolverId);
                    }

                    AppTuple.Save();
                }
                catch (Exception) { Debug.WriteLine($"{nameof(EditAppViewModel)}: Failed to save current selected item."); }
            })
            .DisposeWith(disp);
        });
    }

    public string Name
    {
        get => _appConfig.AppName;
        set => this.RaiseAndSetIfChanged(_appConfig.AppName, value, _appConfig, (m, v) => m.AppName = v);
    }

    public string AppPath
    {
        get => _appConfig.AppLocation;
        set => this.RaiseAndSetIfChanged(_appConfig.AppLocation, value, _appConfig, (m, v) => m.AppLocation = v);
    }

    public string AppArgs
    {
        get => _appConfig.AppArguments;
        set => this.RaiseAndSetIfChanged(_appConfig.AppArguments, value, _appConfig, (m, v) => m.AppArguments = v);
    }

    public string WorkingDir
    {
        get => _appConfig.WorkingDirectory;
        set => this.RaiseAndSetIfChanged(_appConfig.WorkingDirectory, value, _appConfig, (m, v) => m.WorkingDirectory = v);
    }

    public ReloadedMode ReloadedMode
    {
        get => _appConfig.ReloadedMode;
        set => this.RaiseAndSetIfChanged(_appConfig.ReloadedMode, value, _appConfig, (m, v) => m.ReloadedMode = v);
    }

    public ReloadedMode[] Modes { get; } = Enum.GetValues<ReloadedMode>();

    public AppVersion[] Versions { get; } = [];

    public ObservableCollection<ProviderFactoryConfiguration> PackageProviders { get; } = [];

    public PathTuple<ApplicationConfig>? AppTuple { get; set; }

    public MakeShortcutCommand MakeShortcutCommand { get; }

    public OpenPathWithShellCommand OpenFolderCommand { get; } = new();

    public ReactiveCommand<Unit, Unit> DeleteAppCommand { get; }

    [RelayCommand]
    private async Task SelectApp()
    {
        var newAppPath = await CommonInteractions.SaveFile.Handle(new()
        {
            Title = "Select Application",
            Filter = "Applications (*.exe)|*.exe",
            OverwritePrompt = false,
            FileName = ApplicationConfig.GetAbsoluteAppLocation(AppTuple)
        });

        if (string.IsNullOrEmpty(newAppPath))
        {
            return;
        }

        // Resolve SymLink.
        // Original warns if trying to select a *new* application in EditApp
        // instead of AddApp. That's on them yall...
        var fileInfo = new FileInfo(newAppPath);
        if (fileInfo.LinkTarget != null) { newAppPath = fileInfo.LinkTarget; }

        AppPath = newAppPath;
        WorkingDir = Path.GetDirectoryName(newAppPath)!;

        // Handle MsStore stuff
        var isMsStore = TryUnprotectGamePassGame.TryIt(newAppPath);
        if (isMsStore) { ReloadedMode = ReloadedMode.External; }
    }

    [RelayCommand]
    private async Task SelectWorkingDir()
    {
        var dirs = await CommonInteractions.SelectFolder.Handle(new() { Title = "Select Working Directory" });
        if (dirs?.Length > 0)
        {
            WorkingDir = dirs[0];
        }
    }

    [RelayCommand]
    private void OpenAppFolder()
    {
        var openAppDir = new OpenPathWithShellCommand(Path.GetDirectoryName(AppPath));
        if (openAppDir.CanExecute(null)) openAppDir.Execute(null);
    }

    private void DeleteApp()
    {
        var appDir = Path.GetDirectoryName(AppTuple!.Path);
        if (Directory.Exists(appDir))
        {
            AppTuple = null;
            Directory.Delete(appDir, true);
        }
    }
}
