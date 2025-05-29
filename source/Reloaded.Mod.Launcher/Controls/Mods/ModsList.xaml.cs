using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace Reloaded.Mod.Launcher.Controls.Mods;

/// <summary>
/// Interaction logic for ModsList.xaml
/// </summary>
public partial class ModsList : UserControl
{
    public ModsList()
    {
        InitializeComponent();

        ModsListView.Loaded += OnModsListViewOnLoaded;
        ModsListView.Unloaded += ModsListViewOnUnloaded;
    }

    private void ModsListViewOnUnloaded(object sender, RoutedEventArgs e)
    {
        ModsListView.Loaded -= OnModsListViewOnLoaded;
        ModsListView.Unloaded -= ModsListViewOnUnloaded;
    }

    private void OnModsListViewOnLoaded(object sender, RoutedEventArgs _)
    {
        if (VisualTreeHelper.GetChild((ListView)sender, 0) is Border border && VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer)
        {
            scrollViewer.PreviewMouseWheel += (s, args) =>
            {
                if (Orientation != Orientation.Horizontal) return;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - args.Delta);
                args.Handled = true;
            };
        }
    }

    public static readonly DependencyProperty SelectedModProperty =
        DependencyProperty.Register(
            nameof(SelectedMod),
            typeof(object),
            typeof(ModsList),
            new PropertyMetadata()
        );

    public object SelectedMod
    {
        get => GetValue(SelectedModProperty);
        set => SetValue(SelectedModProperty, value);
    }

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(ModsList),
            new PropertyMetadata(false)
        );

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public static readonly DependencyProperty ModsSourceProperty =
        DependencyProperty.Register(
            nameof(ModsSource),
            typeof(object),
            typeof(ModsList),
            new PropertyMetadata()
        );

    public object ModsSource
    {
        get => GetValue(ModsSourceProperty);
        set => SetValue(ModsSourceProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ModsList),
            new PropertyMetadata(Orientation.Vertical)
        );

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
}
