using CommunityToolkit.Mvvm.Input;
using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace Reloaded.Mod.Launcher.Lib.Models.ViewModel.Dialog;

/// <summary>
/// The ViewModel for a dialog which allows us to edit the details of an individual mod.
/// </summary>
public partial class EditModDialogViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// The individual mod configuration to be edited.
    /// </summary>
    public ModConfig Config { get; set; }

    /// <summary>
    /// The individual mod configuration to be edited.
    /// </summary>
    public PathTuple<ModConfig> ConfigTuple { get; set; }

    /// <summary>
    /// All possible dependencies for the mod configurations.
    /// </summary>
    public ObservableCollection<BooleanGenericTuple<IModConfig>> Dependencies { get; set; } = new ObservableCollection<BooleanGenericTuple<IModConfig>>();

    public ObservableCollection<BooleanGenericTuple<IModConfig>> EnabledDependencies { get; set; } = [];

    public ObservableCollection<BooleanGenericTuple<IModConfig>> DisabledDependencies { get; set; } = [];

    /// <summary>
    /// All tags used.
    /// </summary>
    public ObservableCollection<string> Tags { get; set; } = new ObservableCollection<string>();

    /// <summary>
    /// All possible applications for the mod configurations.
    /// </summary>
    public ObservableCollection<BooleanGenericTuple<IApplicationConfig>> Applications { get; set; } = new ObservableCollection<BooleanGenericTuple<IApplicationConfig>>();

    /// <summary>
    /// Name of the tag to be set.
    /// </summary>
    public string TagName { get; set; } = "";

    /// <summary>
    /// Filter allowing for dependencies to be filtered out.
    /// </summary>
    public string ModsFilter { get; set; } = "";

    public string AppsFilter { get; set; } = "";

    /// <summary>
    /// The current page for the modification.
    /// </summary>
    public EditModPage Page { get; set; } = EditModPage.Main;
    
    /// <summary>
    /// True if user can navigate to the last page.
    /// </summary>
    public bool CanGoToLastPage { get; set; } = false;

    /// <summary>
    /// True if user can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage { get; set; } = true;

    /// <summary>
    /// True if user is on the last page.
    /// </summary>
    public bool IsOnLastPage { get; set; } = false;

    /// <summary>
    /// List of all configurable configurations.
    /// </summary>
    public ObservableCollection<ResolverFactoryConfiguration> Updates { get; set; } = new ObservableCollection<ResolverFactoryConfiguration>();

    private readonly ApplicationConfigService _applicationConfigService;
    private SetModImageCommand _setModImageCommand;
    private Action? _close;

    /// <inheritdoc />
    public EditModDialogViewModel(PathTuple<ModConfig> modTuple, ApplicationConfigService applicationConfigService, ModConfigService modConfigService)
    {
        _applicationConfigService = applicationConfigService;
        ConfigTuple = modTuple;
        Config = modTuple.Config;

        // Add Tags
        Tags.AddRange(Config.Tags);

        // Add known applications.
        var apps = applicationConfigService.Items;
        foreach (var app in apps)
        {
            bool isAppEnabled = modTuple.Config.SupportedAppId.Contains(app.Config.AppId, StringComparer.OrdinalIgnoreCase);
            Applications.Add(new BooleanGenericTuple<IApplicationConfig>(isAppEnabled, app.Config));
        }

        // Build Dependencies
        var mods = modConfigService.Items; // In case collection changes during window open.
        foreach (var mod in mods)
        {
            bool isModEnabled = modTuple.Config.ModDependencies.Contains(mod.Config.ModId, StringComparer.OrdinalIgnoreCase);
            var dep = new BooleanGenericTuple<IModConfig>(isModEnabled, mod.Config);
            Dependencies.Add(dep);

            if (isModEnabled)
            {
                EnabledDependencies.Add(dep);
            }
            else
            {
                DisabledDependencies.Add(dep);
            }

            // Add unknown applications from mods.
            foreach (var appId in mod.Config.SupportedAppId)
            {
                bool isAppEnabled = modTuple.Config.SupportedAppId.Contains(appId, StringComparer.OrdinalIgnoreCase);
                if (!Applications.Any(x => x.Generic.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase)))
                {
                    Applications.Add(new BooleanGenericTuple<IApplicationConfig>(isAppEnabled, new UnknownApplicationConfig(appId)));
                }
            }
        }

        // Build Update Configurations
        foreach (var resolver in PackageResolverFactory.All)
        {
            var result = ResolverFactoryConfiguration.TryCreate(resolver, ConfigTuple);
            if (result != null)
                Updates.Add(result);
        }

        // Everything Else
        _setModImageCommand = new SetModImageCommand(modTuple);
        IoC.RebindToConstant(this);
        PropertyChanged += OnPageChanged;
    }

    /// <summary>
    /// Disposes resources of this window.
    /// </summary>
    public void Dispose() => IoC.Unbind(this);

    /// <summary>
    /// Initializes the window with an action to close it.
    /// </summary>
    public void Init(Action close) => _close = close;

    /// <summary>
    /// Saves the mod back to the mod directory.
    /// </summary>
    public void Save()
    {
        // Make folder path and save folder.
        string modDirectory = Path.GetDirectoryName(ConfigTuple.Path)!;

        // Save Config
        string configSavePath  = Path.Combine(modDirectory, ModConfig.ConfigFileName);
        Config.ModDependencies = EnabledDependencies.Select(x => x.Generic.ModId).ToArray();
        Config.SupportedAppId = Applications.Where(x => x.Enabled).Select(x => x.Generic.AppId).ToArray();
        Config.Tags = Tags.ToArray();

        // Save Plugins
        foreach (var update in Updates)
        {
            if (update.IsEnabled)
                update.Factory.SetConfiguration(ConfigTuple, update.Configuration);
            else
                Config.PluginData.Remove(update.Factory.ResolverId);
        }

        ConfigReader<ModConfig>.WriteConfiguration(configSavePath, (ModConfig) Config);
    }

    /* Get Image To Display */

    /// <summary>
    /// Filters an individual item.
    /// Returns true if the item should pass by the filter, else false.
    /// </summary>
    public bool FilterMod(BooleanGenericTuple<IModConfig> item)
    {
        if (ModsFilter.Length <= 0)
            return true;
        
        return item.Generic.ModName.IndexOf(ModsFilter, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }

    /// <summary>
    /// Filters an individual item.
    /// Returns true if the item should pass by the filter, else false.
    /// </summary>
    public bool FilterApp(BooleanGenericTuple<IApplicationConfig> item)
    {
        if (AppsFilter.Length <= 0)
            return true;

        var appNameResult = item.Generic.AppName.IndexOf(AppsFilter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        var appIdResult = item.Generic.AppId.IndexOf(AppsFilter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        return appNameResult || appIdResult;
    }

    /// <summary>
    /// Sets a new mod image.
    /// </summary>
    public void SetNewImage()
    {
        if (_setModImageCommand.CanExecute(null))
            _setModImageCommand.Execute(null);
    }

    /// <summary>
    /// Adds the currently written in tag.
    /// </summary>
    public void AddCurrentTag()
    {
        if (string.IsNullOrEmpty(TagName) || Tags.Contains(TagName))
            return;

        Tags.Add(TagName);
        TagName = "";
    }

    /// <summary>
    /// Used to close the page/window associated with the dialog.
    /// </summary>
    public void Close() => _close?.Invoke();

    [SuppressPropertyChangedWarnings]
    private void OnPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Page))
        {
            CanGoToLastPage = Page > EnumValues<EditModPage>.Min;
            CanGoToNextPage = Page < EnumValues<EditModPage>.Max;
            IsOnLastPage = Page == EnumValues<EditModPage>.Max;
            ModsFilter = string.Empty;
        }
    }

    [RelayCommand]
    private void AddRemoveDep(string depId)
    {
        var enabledDep = EnabledDependencies.FirstOrDefault(x => x.Generic.ModId == depId);
        if (enabledDep != null)
        {
            EnabledDependencies.Remove(enabledDep);
            DisabledDependencies.Add(enabledDep);
            return;
        }

        var disabledDep = DisabledDependencies.FirstOrDefault(x => x.Generic.ModId == depId);
        if (disabledDep != null)
        {
            DisabledDependencies.Remove(disabledDep);
            EnabledDependencies.Add(disabledDep);
        }
    }
}