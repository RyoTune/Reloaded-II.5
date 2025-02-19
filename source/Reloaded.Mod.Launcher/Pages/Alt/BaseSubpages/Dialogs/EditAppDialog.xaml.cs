using ReactiveUI;
using System.Reactive.Disposables;
using EditAppViewModel = Reloaded.Mod.Launcher.Lib.Remix.ViewModels.EditAppViewModel;

namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.Dialogs;

/// <summary>
/// Interaction logic for EditAppDialog.xaml
/// </summary>
public partial class EditAppDialog : ReactiveWindow<EditAppViewModel>
{
    public EditAppDialog()
    {
        InitializeComponent();

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.DataContext = this.ViewModel;

            this.ViewModel!.DeleteAppCommand
            .Subscribe(_ => this.Close())
            .DisposeWith(disp);
        });
    }
}
