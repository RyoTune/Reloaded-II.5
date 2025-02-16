using CommunityToolkit.Mvvm.ComponentModel;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

namespace Reloaded.Mod.Launcher.Lib.Models.ViewModel;

/// <summary>
/// View Model for the main application window.
/// </summary>
public partial class WindowViewModel : ViewModelBase
{
    /// <summary>
    /// The currently displayed page on this window.
    /// </summary>
    public PageBase CurrentPage
    {
        get;
        set;
    } = PageBase.Splash;

    [ObservableProperty]
    private string _windowTitle = "Reloaded II.5 ReMIX";
}