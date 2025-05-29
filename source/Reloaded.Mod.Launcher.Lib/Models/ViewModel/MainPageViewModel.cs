using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Reloaded.Mod.Launcher.Lib.Remix.Extensions;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using System.Reactive.Linq;
using Page = Reloaded.Mod.Launcher.Lib.Models.Model.Pages.Page;

namespace Reloaded.Mod.Launcher.Lib.Models.ViewModel;

/// <summary>
/// A viewmodel for the 'main page', which consists of the sidebar and a secondary, child page on the right panel.
/// </summary>
public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private PathTuple<ApplicationConfig>? _selectedApplication;

    /// <summary>
    /// Stores the page to be displayed to the user.
    /// </summary>
    [DoNotNotify]
    public Page Page
    {
        get => _launcherPage;
        set
        {
            if (_launcherPage == value) 
                return;

            _launcherPage = value;
            this.OnPropertyChanged(nameof(Page));
            SelectedApplication = null;
        }
    }
    
    /// <summary>
    /// Allows us to add an application.
    /// </summary>
    public AddApplicationCommand AddApplicationCommand      { get; private set; }
    
    /// <summary>
    /// Service providing access to all application configurations.
    /// </summary>
    public ApplicationConfigService ConfigService           { get; private set; }
    
    // Note: Do not merge with property. Used below.
    private Page _launcherPage = Page.SettingsPage;

    private readonly AutoInjector _autoInjector;

    /// <inheritdoc />
    public MainPageViewModel(ApplicationConfigService service)
    {
        ConfigService = service;
        AddApplicationCommand = new AddApplicationCommand(this, service);
        _autoInjector = new AutoInjector(service);

        NotifyInvalidApps(service);

        var appsChangeSet = this.ConfigService.Items.ToObservableChangeSet();
        appsChangeSet.AutoRefresh()
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(_ =>
            {
                if (this.SelectedApplication == null) return;

                if (this.ConfigService.Items.All(x => x.Config.AppId != this.SelectedApplication.Config.AppId))
                {
                    this.Page = Page.SettingsPage;
                }
            });
    }

    private static void NotifyInvalidApps(ApplicationConfigService configService)
    {
        foreach (var app in configService.Items.Where(x => !File.Exists(x.Config.AppLocation)))
        {
            CommonInteractions.Toast.Handle(new()
            {
                Type = ToastConfig.ToastType.Prompt,
                CancelText = "Ignore",
                ConfirmText = "Fix",
                Message = $"ERROR: {app.Config.AppName}\nThe application EXE was not found!",

                PromptFunc = result => !result || app.SelectAppPath()
            }).Wait();
        }
    }

    /// <summary>
    /// Changes the Application page to display and displays the application.
    /// </summary>
    public void SwitchToApplication(PathTuple<ApplicationConfig> tuple)
    {
        if (this.SelectedApplication == tuple) return;

        SelectedApplication = tuple;
        _launcherPage = Page.Application;
        this.OnPropertyChanged(nameof(Page));
    }

    /// <summary>
    /// Switches page in a given direction.
    /// </summary>
    /// <param name="direction">The direction to switch page in. Expected values -1, 0, 1</param>
    /// <param name="sortedConfigurations">List of all sorted applications.</param>
    public void SwitchPage(int direction, IList<PathTuple<ApplicationConfig>> sortedConfigurations)
    {
        direction = NavigationUtils.NormalizeDirection(direction);

        // Get index of application
        var appIndex = sortedConfigurations.IndexOf(SelectedApplication!);
        if (appIndex == -1)
            appIndex = 0;

        // Add to current page.
        var firstAppIndex = (int)Page.Application;
        var pageCount  = firstAppIndex + sortedConfigurations.Count;
        var nextIndex = ((int)Page + appIndex + direction) % (pageCount);
        if (nextIndex < 0)
            nextIndex = pageCount - (-nextIndex); // + 1 to convert page index to page count

        // Handle regular page.
        if (nextIndex < firstAppIndex)
        {
            Page = (Page)nextIndex;
            return;
        }

        // Handle application page.
        var appOffset = nextIndex - firstAppIndex;
        SwitchToApplication(sortedConfigurations[appOffset]);
    }
}