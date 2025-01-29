namespace Reloaded.Mod.Launcher.Pages.Alt.BaseSubpages.Dialogs;

/// <summary>
/// Interaction logic for EditModDialog.xaml
/// </summary>
public partial class EditModDialog : ReloadedWindow
{
    public EditModDialogViewModel RealViewModel { get; set; }

    private readonly CollectionViewSource _dependenciesViewSource;

    private readonly CollectionViewSource _appsViewSource;

    public EditModDialog(EditModDialogViewModel vm)
    {
        InitializeComponent();

        RealViewModel = vm;
        RealViewModel.Init(this.Close);

        _dependenciesViewSource = new DictionaryResourceManipulator(this.Grid.Resources).Get<CollectionViewSource>("SortedDependencies");
        _dependenciesViewSource.Filter += DependenciesViewSourceOnFilter;

        _appsViewSource = new DictionaryResourceManipulator(this.Grid.Resources).Get<CollectionViewSource>("SortedApplications");
        _appsViewSource.Filter += AppsViewSourceOnFilter;

        this.Closing += OnClosing;
    }

    private void ModIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        RealViewModel.SetNewImage();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        RealViewModel.Save();
        RealViewModel.Dispose(); // Unbind Constant.
    }

    private void ModsFilter_TextChanged(object sender, TextChangedEventArgs e) => _dependenciesViewSource.View.Refresh();

    private void DependenciesViewSourceOnFilter(object sender, FilterEventArgs e) => e.Accepted = RealViewModel.FilterMod((BooleanGenericTuple<IModConfig>)e.Item);

    private void AppsFilter_TextChanged(object sender, TextChangedEventArgs e) => _appsViewSource.View.Refresh();

    private void AppsViewSourceOnFilter(object sender, FilterEventArgs e) => e.Accepted = RealViewModel.FilterApp((BooleanGenericTuple<IApplicationConfig>)e.Item);

    private void Button_Click(object sender, RoutedEventArgs e) => this.Close();
}
