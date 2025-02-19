using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Commands;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using Version = Reloaded.Mod.Launcher.Lib.Utility.Version;

namespace Reloaded.Mod.Launcher.Lib.Models.ViewModel;

/// <summary>
/// ViewModel for the Settings Page.
/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class SettingsPageViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly MainPageViewModel _mainPage;

    /// <summary>
    /// Provides access to all available applications.
    /// </summary>
    public ApplicationConfigService AppConfigService { get; set; }

    /// <summary>
    /// Provides access to all available mods.
    /// </summary>
    public ModConfigService ModConfigService { get; set; }

    /// <summary>
    /// Number of total applications installed.
    /// </summary>
    public int TotalApplicationsInstalled { get; set; }

    /// <summary>
    /// Number of total mods installed.
    /// </summary>
    public int TotalModsInstalled { get; set; }

    /// <summary>
    /// Copyright string to display on the Settings Panel.
    /// </summary>
    public string Copyright { get; set; }

    /// <summary>
    /// Configuration for the mod loader.
    /// </summary>
    public LoaderConfig LoaderConfig { get; set; }

    /// <summary>
    /// Allows you to select the mod loader language.
    /// </summary>
    public IResourceFileSelector? LanguageSelector => Lib.LanguageSelector;

    /// <summary>
    /// Allows you to select the mod loader theme.
    /// </summary>
    public IResourceFileSelector? ThemeSelector => Lib.ThemeSelector;

    public ViewModelActivator Activator { get; } = new();

    /// <summary/>
    public SettingsPageViewModel(ApplicationConfigService appConfigService, ModConfigService modConfigService, LoaderConfig loaderConfig, MainPageViewModel mainPage)
    {
        _mainPage = mainPage;

        AppConfigService = appConfigService;
        ModConfigService = modConfigService;
        LoaderConfig = loaderConfig;

        UpdateTotalApplicationsInstalled();
        UpdateTotalModsInstalled();
        AppConfigService.Items.CollectionChanged += MainPageViewModelOnApplicationsChanged;
        ModConfigService.Items.CollectionChanged += ManageModsViewModelOnModsChanged;

        string copyRightStr = "Sewer56 ~ Unknown Date | Unknown Version";
        try
        {
            var version = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule!.FileName!);
            if (!string.IsNullOrEmpty(version.LegalCopyright))
                copyRightStr = version.LegalCopyright;
        }
        catch (Exception) { /* Non-critical, could happen on CIFS file share, we can ignore. */ }
        
        copyRightStr = Regex.Replace(copyRightStr, @"\|.*", $"| {Version.GetReleaseVersion()!.ToNormalizedString()}");
        copyRightStr += $" | {RuntimeInformation.FrameworkDescription}";
        Copyright = copyRightStr;
    }

    [RelayCommand]
    private void OpenApp(PathTuple<ApplicationConfig> tuple)
    {
        _mainPage.SwitchToApplication(tuple);
    }

    [RelayCommand]
    private void OpenAppFolder()
    {
        var openAppDir = new OpenPathWithShellCommand(Path.GetDirectoryName(this.LoaderConfig.ApplicationConfigDirectory));
        if (openAppDir.CanExecute(null)) openAppDir.Execute(null);
    }

    /// <summary>
    /// Asynchronously saves the loader config.
    /// </summary>
    public async Task SaveConfigAsync()
    {
        await IConfig<LoaderConfig>.ToPathAsync(LoaderConfig, Paths.LoaderConfigPath);
    }

    /// <summary>
    /// Asynchronously saves the new chosen language.
    /// </summary>
    public async Task SaveNewLanguageAsync()
    {
        if (LanguageSelector?.File != null)
        {
            LoaderConfig.LanguageFile = LanguageSelector.File;
            await SaveConfigAsync();
        }
    }

    /// <summary>
    /// Asynchronously starts saving new themes.
    /// </summary>
    public async Task SaveNewThemeAsync()
    {
        if (ThemeSelector?.File != null)
        {
            LoaderConfig.ThemeFile = ThemeSelector.File;
            await SaveConfigAsync();
            
            // TODO: This is a bug workaround for where the language ComboBox gets reset after a theme change.
            LanguageSelector!.SelectXamlFileByName(Path.GetFileName(LoaderConfig.LanguageFile!));
        }
    }

    /// <summary>
    /// Opens the location where the log files are stored.
    /// </summary>
    public void OpenLogFileLocation() => ProcessExtensions.OpenFileWithDefaultProgram(Paths.LogPath);
    
    /// <summary>
    /// Opens the main Reloaded configuration file.
    /// </summary>
    public void OpenConfigFile() => ProcessExtensions.OpenFileWithDefaultProgram(Paths.LoaderConfigPath);

    /* Functions */
    private void UpdateTotalApplicationsInstalled() => TotalApplicationsInstalled = AppConfigService.Items.Count;
    private void UpdateTotalModsInstalled() => TotalModsInstalled = ModConfigService.Items.Count;

    /* Events */
    private void ManageModsViewModelOnModsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateTotalModsInstalled();
    private void MainPageViewModelOnApplicationsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateTotalApplicationsInstalled();
}