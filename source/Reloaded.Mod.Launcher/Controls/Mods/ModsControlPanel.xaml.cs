using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using System.Reactive.Disposables;

namespace Reloaded.Mod.Launcher.Controls.Mods;

/// <summary>
/// Interaction logic for ModsControlPanel.xaml
/// </summary>
public partial class ModsControlPanel : ReactiveUserControl<ModsControlPanelViewModel>
{
    public ModsControlPanel()
    {
        InitializeComponent();

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.ViewModel = this.DataContext as ModsControlPanelViewModel;

            this.ViewModel!.Presets.ToObservableChangeSet()
                .AutoRefresh()
                .SkipInitial()
                .Subscribe(_ =>
                {
                    this.ListView_Presets.Items.Refresh();
                })
                .DisposeWith(disp);

            this.ViewModel!.WhenPropertyChanged(x => x.ShortcutsEnabled, false)
                .Subscribe(_ => this.ListView_Presets.Items.Refresh())
                .DisposeWith(disp);
        });
    }
}
