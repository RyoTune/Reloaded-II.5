using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Commands;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Launcher.Lib.Remix.Utils;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class EditAppViewModel : ViewModelBase, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly ApplicationConfig _appConfig;

    public EditAppViewModel(PathTuple<ApplicationConfig> appTuple)
    {
        this.AppTuple = appTuple;
        _appConfig = appTuple.Config;

        this.MakeShortcutCommand = new(appTuple, Lib.IconConverter);
        this.DeleteAppCommand = ReactiveCommand.Create(this.DeleteApp);

        // Build Package Provider Configurations
        foreach (var provider in PackageProviderFactory.All)
        {
            var result = ProviderFactoryConfiguration.TryCreate(provider, appTuple);
            if (result != null)
                this.PackageProviders.Add(result);
        }

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(_ => this.AppTuple.Save())
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
                            if (!AsiLoader.TryRemoveAsi(this.AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to remove.
                            }
                        }
                        break;
                    case ReloadedMode.External:
                        {
                            _appConfig.DontInject = true;
                            _appConfig.AutoInject = false;
                            if (!AsiLoader.TryDeployAsi(this.AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to deploy.
                            }
                        }
                        break;
                    case ReloadedMode.AutoInject:
                        {
                            _appConfig.DontInject = true;
                            _appConfig.AutoInject = true;
                            if (!AsiLoader.TryRemoveAsi(this.AppPath, out var loaderPath, out var bootstrapperPath))
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
                            if (!AsiLoader.TryRemoveAsi(this.AppPath, out var loaderPath, out var bootstrapperPath))
                            {
                                // TODO: Notify failed to remove.
                            }
                        }
                        break;
                }

                this.AppTuple?.Save();
            })
            .DisposeWith(disp);

            Disposable.Create(() =>
            {
                if (this.AppTuple == null) return;

                try
                {
                    // Save Plugins
                    foreach (var provider in PackageProviders)
                    {
                        if (provider.IsEnabled)
                            provider.Factory.SetConfiguration(this.AppTuple, provider.Configuration);
                        else
                            this.AppTuple.Config.PluginData.Remove(provider.Factory.ResolverId);
                    }

                    this.AppTuple.Save();
                }
                catch (Exception) { Debug.WriteLine($"{nameof(EditAppViewModel)}: Failed to save current selected item."); }
            })
            .DisposeWith(disp);
        });
    }

    public string Name
    {
        get => _appConfig.AppName;
        set => this.SetProperty(_appConfig.AppName, value, _appConfig, (m, v) => m.AppName = v);
    }

    public string AppPath
    {
        get => _appConfig.AppLocation;
        set => this.SetProperty(_appConfig.AppLocation, value, _appConfig, (m, v) =>
        {
            m.AppLocation = v;
            this.RaisePropertyChanged(nameof(AppPath));
        });
    }

    public string AppArgs
    {
        get => _appConfig.AppArguments;
        set => this.SetProperty(_appConfig.AppArguments, value, _appConfig, (m, v) => m.AppArguments = v);
    }

    public string WorkingDir
    {
        get => _appConfig.WorkingDirectory;
        set => this.SetProperty(_appConfig.WorkingDirectory, value, _appConfig, (m, v) => m.WorkingDirectory = v);
    }

    public ReloadedMode ReloadedMode
    {
        get => _appConfig.ReloadedMode;
        set
        {
            this.SetProperty(_appConfig.ReloadedMode, value, _appConfig, (m, v) =>
            {
                m.ReloadedMode = v;
                this.RaisePropertyChanged(nameof(ReloadedMode));
            });
        }
    }

    public ReloadedMode[] Modes { get; } = Enum.GetValues<ReloadedMode>();

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

        this.AppPath = newAppPath;
        this.WorkingDir = Path.GetDirectoryName(newAppPath)!;

        // Handle MsStore stuff
        var isMsStore = TryUnprotectGamePassGame.TryIt(newAppPath);
        if (isMsStore) { this.ReloadedMode = ReloadedMode.External; }
    }

    [RelayCommand]
    private async Task SelectWorkingDir()
    {
        var dirs = await CommonInteractions.SelectFolder.Handle(new() { Title = "Select Working Directory" });
        if (dirs?.Length > 0)
        {
            this.WorkingDir = dirs[0];
        }
    }

    [RelayCommand]
    private void OpenAppFolder()
    {
        var openAppDir = new OpenPathWithShellCommand(Path.GetDirectoryName(this.AppPath));
        if (openAppDir.CanExecute(null)) openAppDir.Execute(null);
    }

    private void DeleteApp()
    {
        var appDir = Path.GetDirectoryName(this.AppTuple!.Path);
        if (Directory.Exists(appDir))
        {
            this.AppTuple = null;
            Directory.Delete(appDir, true);
        }
    }
}
