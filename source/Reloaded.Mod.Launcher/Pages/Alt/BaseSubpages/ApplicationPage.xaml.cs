using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.Dialogs;
using System.Reactive.Linq;
using ApplicationSubPage = Reloaded.Mod.Launcher.Lib.Models.Model.Pages.ApplicationSubPage;
using EditAppViewModel = Reloaded.Mod.Launcher.Lib.Remix.ViewModels.EditAppViewModel;
using EditModDialog = Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.Dialogs.EditModDialog;
using Environment = Reloaded.Mod.Shared.Environment;
using Window = System.Windows.Window;
using WindowViewModel = Reloaded.Mod.Launcher.Lib.Models.ViewModel.WindowViewModel;

namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages;

/// <summary>
/// Interaction logic for ApplicationPage.xaml
/// </summary>
public partial class ApplicationPage : ReloadedIIPage, IDisposable
{
    public ApplicationViewModel ViewModel { get; set; }

    private bool _disposed;

    public ApplicationPage()
    {
        SwappedOut += () =>
        {
            ViewModel?.ChangeApplicationPage(ApplicationSubPage.Null); // make sure swappedout is triggered when switching tabs
            Dispose();
        };
        
        InitializeComponent();
        ViewModel = new ApplicationViewModel(Lib.IoC.Get<MainPageViewModel>().SelectedApplication!, Lib.IoC.Get<ModConfigService>(), Lib.IoC.Get<ModUserConfigService>(), Lib.IoC.Get<LoaderConfig>());
        ViewModel.PropertyChanged += WhenPageChanged;
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ApplicationSummary);

        var appSumPage = this.PageHost.CurrentPage as ApplicationSubPages.AppSummaryPage;
        for (int i = 0; i < 9; i++)
        {
            var id = i + 1;
            var inputBinding = new KeyBinding(ReactiveCommand.Create(() =>
            {
                appSumPage?.ViewModel.ApplyShortcut(id);
                Keyboard.Focus(this);
            }), Enum.Parse<Key>($"D{id}"), ModifierKeys.Control);
            this.InputBindings.Add(inputBinding);
        }
    }
    
    private void WhenPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Page))
            SwitchPage(ViewModel.Page);
    }

    ~ApplicationPage()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ViewModel.PropertyChanged -= WhenPageChanged;
        ActionWrappers.ExecuteWithApplicationDispatcherAsync(() =>
        {
            if (PageHost.CurrentPage is IDisposable disposable)
                disposable.Dispose();

            PageHost.CurrentPage?.AnimateOut();
        });
        ViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ReloadedMod_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Process process }) 
            return;

        ViewModel.SelectedProcess = process;
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ReloadedProcess);
    }

    private void NonReloadedMod_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Process process }) 
            return;

        ViewModel.SelectedProcess = process;
        if (!process.HasExited)
            ViewModel.ChangeApplicationPage(ApplicationSubPage.NonReloadedProcess);
    }

    private void Summary_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ApplicationSummary);
    }

    private void Button_OpenSettings(object sender, MouseButtonEventArgs e)
    {
        var editAppDialog = new EditAppDialog(new EditAppViewModel(this.ViewModel.ApplicationTuple));
        editAppDialog.ShowDialog();
    }

    private async void LaunchApplication_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Let's not crash when user has invalid path etc.
        try
        {
            await ViewModel.ApplicationTuple.SaveAsync();
            ViewModel.EnforceModCompatibility();
            await Setup.CheckForMissingModDependenciesAsync();

            var appTuple = ViewModel.ApplicationTuple;
            var launcher  = ApplicationLauncher.FromApplicationConfig(appTuple);

            if (!Environment.IsWine || (Environment.IsWine && CompatibilityDialogs.WineShowLaunchDialog()))
                launcher.Start(!appTuple.Config.DontInject);
        }
        catch (Exception ex)
        {
            Errors.HandleException(ex);
        }
    }

    private void MakeShortcut_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.MakeShortcutCommand.CanExecute(null))
            ViewModel.MakeShortcutCommand.Execute(null);
    }

    private void LoadModSet_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ViewModel.LoadModSet();
    private void SaveModSet_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ViewModel.SaveModSet();

    /* Animation/Title/Setup overrides */
    protected override Animation[] MakeExitAnimations()
    {
        return new Animation[]
        {
            new RenderTransformAnimation(-this.ActualWidth, RenderTransformDirection.Horizontal, RenderTransformTarget.Away, null, XamlExitSlideAnimationDuration.Get()),
            new OpacityAnimation(XamlExitFadeAnimationDuration.Get(), 1, XamlExitFadeOpacityEnd.Get())
        };
    }

    protected override void OnAnimateInFinished()
    {
        Lib.IoC.Get<WindowViewModel>().WindowTitle = $"Reloaded II.5 ReMIX ({ViewModel.ApplicationTuple.Config.AppName})";
    }

    // Switch to new page.
    private void SwitchPage(ApplicationSubPage page)
    {
        PageHost.CurrentPage = page switch
        {
            ApplicationSubPage.Null => null,
            ApplicationSubPage.NonReloadedProcess => new NonReloadedProcessPage(ViewModel),
            ApplicationSubPage.ReloadedProcess => new ReloadedProcessPage(ViewModel),
            ApplicationSubPage.ApplicationSummary => new ApplicationSubPages.AppSummaryPage(ViewModel),
            ApplicationSubPage.EditApplication => new EditAppPage(ViewModel),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }

    private async void Create_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var newModName = await CommonInteractions.PromptTextInput.Handle(new() { Title = "Create Mod", Description = "Enter a name for the new mod." });
        if (string.IsNullOrEmpty(newModName))
        {
            return;
        }

        var presetConfig = new ModConfig()
        {
            ModName = newModName,
            SupportedAppId = [this.ViewModel.ApplicationTuple.Config.AppId],
        };

        var createModVm = new EditModViewModel(Lib.IoC.Get<ApplicationConfigService>(), Lib.IoC.Get<ModConfigService>(), presetConfig);
        var createModDialog = new EditModDialog(createModVm);
        createModDialog.Owner = Window.GetWindow(this);
        createModDialog.ShowDialog();
    }
}