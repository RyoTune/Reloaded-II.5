using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Disposables;
using ApplicationSubPage = Reloaded.Mod.Launcher.Pages.BaseSubpages.ApplicationSubPages.ApplicationSubPage;
using Button = Sewer56.UI.Controller.Core.Enums.Button;

namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.ApplicationSubPages;

/// <summary>
/// Interaction logic for ApplicationSummaryPage.xaml
/// </summary>
[IViewFor<ConfigureModsViewModel>]
public partial class AppSummaryPage : ApplicationSubPage
{
    private readonly DictionaryResourceManipulator _manipulator;
    private readonly CollectionViewSource _modsViewSource;
    private bool _disposed;

    public AppSummaryPage(ApplicationViewModel appViewModel)
    {
        InitializeComponent();

        _manipulator = new DictionaryResourceManipulator(this.Contents.Resources);
        _modsViewSource = _manipulator.Get<CollectionViewSource>("FilteredMods");

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.ViewModel = new ConfigureModsViewModel(appViewModel, Lib.IoC.Get<ModUserConfigService>(), Lib.IoC.Get<LoaderConfig>());

            ControllerSupport.SubscribeCustomInputs(OnProcessCustomInputs);

            _modsViewSource.Filter += ModsViewSourceOnFilter;
            this.ViewModel.PropertyChanged += OnFilterChanged;
            this.ViewModel.ToggleModHideCommand.Subscribe(_ => _modsViewSource.View.Refresh()).DisposeWith(disp);

            Disposable.Create(() =>
            {
                ControllerSupport.UnsubscribeCustomInputs(OnProcessCustomInputs);
            })
            .DisposeWith(disp);
        });
    }

    private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedTag))
            _modsViewSource.View.Refresh();

        if (e.PropertyName == nameof(ViewModel.ShowHidden))
            _modsViewSource.View.Refresh();
    }

    private void ModsViewSourceOnFilter(object sender, FilterEventArgs e)
    {;
        var tuple = (ModEntry)e.Item;
        e.Accepted = true;

        if (string.IsNullOrEmpty(ModsFilter.Text))
            return;

        if (tuple.IsHidden && this.ViewModel?.ShowHidden == false)
        {
            e.Accepted = false;
            return;
        }

        if (tuple.Tuple.Config.IsSeparator)
        {
            e.Accepted = false;
            return;
        }

        // Filter name
        var config = tuple.Tuple.Config;
        if (ModsFilter.Text.Length > 0)
            e.Accepted = config.ModName.Contains(ModsFilter.Text, StringComparison.InvariantCultureIgnoreCase);

        if (e.Accepted == false)
            return;

        // Filter tag
        if (ViewModel.SelectedTag != ConfigureModsViewModel.IncludeAllTag)
        {
            e.Accepted = config.Tags.Contains(ViewModel.SelectedTag);

            if (e.Accepted != false)
                return;

            // Try auto tags
            bool hasCodeInjection = config.HasDllPath();
            if (hasCodeInjection && ViewModel.SelectedTag == ConfigureModsViewModel.CodeInjectionTag)
            {
                e.Accepted = true;
                return;
            }
            else if (!hasCodeInjection && ViewModel.SelectedTag == ConfigureModsViewModel.NoCodeInjectionTag)
            {
                e.Accepted = true;
                return;
            }

            // Auto tag: Universal mod
            if (ViewModel.SelectedTag == ConfigureModsViewModel.NoUniversalModsTag)
                e.Accepted = !config.IsUniversalMod;

            if (ViewModel.SelectedTag == ConfigureModsViewModel.NativeModTag)
                e.Accepted = config.IsNativeMod("");
        }
    }

    private void ModsFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _modsViewSource.View.Refresh();
    }

    #region Keyboard Controls
    private void KeyboardControls_KeyDown(object sender, KeyEventArgs e)
    {
        // Toggle On/Off
        if (e.Key == KeyboardUtils.Accept)
            ToggleModItem();

        ProcessKeyboardItemShift(sender, e);
    }

    private static void ProcessKeyboardItemShift(object sender, KeyEventArgs e)
    {
        // Shift item up/down
        if (!KeyboardUtils.TryGetListScrollDirection(e, out int indexOffset))
            return;

        if (!TryShiftSelectedItem((ListView)sender, indexOffset))
            return;

        // Needed so our selection doesn't skip
        e.Handled = true;
    }
    #endregion

    #region Controller Controls

    private void OnProcessCustomInputs(in ControllerState state, ref bool handled)
    {
        if (!WpfUtilities.TryGetFocusedElementAndWindow(out var window, out var focused))
            return;

        // We only deal with the listview.
        if (focused is not ListViewItem item)
            return;
        
        if (state.IsButtonPressed(Button.Accept))
            ToggleModItem();

        ProcessControllerItemShift(state, item);
    }

    private static void ProcessControllerItemShift(in ControllerState state, ListViewItem listViewItem)
    {
        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        if (!state.IsButtonHeld(Button.Modifier))
            return;

        if (!ControllerSupport.TryGetListScrollDirection(state, out int indexOffset))
            return;

        var listView = WpfUtilities.FindParent<ListView>(listViewItem!);
        if (listView == null)
            return;

        TryShiftSelectedItem(listView, indexOffset);
    }
    #endregion

    private static bool TryShiftSelectedItem(ListView listView, int indexOffset)
    {
        if (!listView.ShiftItem(indexOffset))
            return false;

        KeyboardNav.Focus((UIElement)listView.ItemContainerGenerator.ContainerFromIndex(listView.SelectedIndex));
        return true;
    }

    private void ToggleModItem()
    {
        var mod = ViewModel.SelectedMod;
        if (mod == null || mod.Enabled == null)
            return;

        mod.Enabled = !mod.Enabled;
    }
}