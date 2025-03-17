using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using System.Reactive.Disposables;

namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.Dialogs;

/// <summary>
/// Interaction logic for EditModDialog.xaml
/// </summary>
public partial class EditModDialog : ReactiveWindow<EditModViewModel>
{
    private readonly CollectionViewSource _appsViewSource;
    private readonly CollectionViewSource _modsViewSource;

    public EditModDialog(EditModViewModel vm)
    {
        InitializeComponent();

        this.DataContext = vm;
        this.ViewModel = vm;

        _appsViewSource = new DictionaryResourceManipulator(this.Resources).Get<CollectionViewSource>("SortedApplications");
        _appsViewSource.Filter += AppsFilter;

        _modsViewSource = new DictionaryResourceManipulator(this.Resources).Get<CollectionViewSource>("SortedMods");
        _modsViewSource.Filter += ModsFilter;

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.ViewModel.ConfirmModCommand?.Subscribe(_ => this.Close()).DisposeWith(disp);
            this.ViewModel.WhenPropertyChanged(vm => vm.AppsFilter).Subscribe(_ => _appsViewSource.View.Refresh()).DisposeWith(disp);
            this.ViewModel.WhenPropertyChanged(vm => vm.ModsFilter).Subscribe(_ => _modsViewSource.View.Refresh()).DisposeWith(disp);
        });
    }

    private void AppsFilter(object sender, FilterEventArgs e) => e.Accepted = ViewModel!.FilterApp((BooleanGenericTuple<IApplicationConfig>)e.Item);

    private void ModsFilter(object sender, FilterEventArgs e) => e.Accepted = ViewModel!.FilterMod((BooleanGenericTuple<ModConfig>)e.Item);
}

