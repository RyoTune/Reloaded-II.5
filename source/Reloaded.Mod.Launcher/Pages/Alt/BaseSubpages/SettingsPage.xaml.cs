using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Window = System.Windows.Window;

namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
[IViewFor<SettingsPageViewModel>]
public partial class SettingsPage : ReloadedIIPage
{
    public SettingsPage()
    {
        InitializeComponent();

        this.ViewModel = Lib.IoC.GetConstant<SettingsPageViewModel>();
        this.DataContext = ViewModel;

        this.WhenActivated((CompositeDisposable disp) =>
        {
            var mw = Lib.IoC.Get<MainWindow>();

            // VM should be saving on any changes made instead tbh.
            var saveOnClose = Observable.FromEventPattern(mw, nameof(mw.Closing))
            .Subscribe(async _ => await ViewModel.SaveConfigAsync())
            .DisposeWith(disp);

            Disposable.Create(async () =>
            {
                await ViewModel.SaveConfigAsync();
            })
            .DisposeWith(disp);
        });
    }

    private void Documents_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => new OpenDocumentationCommand().Execute(null);
    private async void LanguageChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => await ViewModel.SaveNewLanguageAsync();
    private async void ThemeChanged(object sender, SelectionChangedEventArgs e) => await ViewModel.SaveNewThemeAsync();

    private void Discord_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ProcessExtensions.OpenFileWithDefaultProgram("https://discord.gg/A8zNnS6");
    private void Twitter_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ProcessExtensions.OpenFileWithDefaultProgram("https://twitter.com/TheSewer56");
    private void Donate_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ProcessExtensions.OpenFileWithDefaultProgram("https://github.com/sponsors/Sewer56");

    private void LogFiles_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ViewModel.OpenLogFileLocation();
    private void ConfigFile_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ViewModel.OpenConfigFile();

    private void Tutorial_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var firstLaunchWindow = new FirstLaunch();
        firstLaunchWindow.Owner = Window.GetWindow(this);
        firstLaunchWindow.ShowDialog();
    }

    private void ControllerConfig_Click(object sender, RoutedEventArgs e) => ControllerSupport.Controller.Configure(true);
}