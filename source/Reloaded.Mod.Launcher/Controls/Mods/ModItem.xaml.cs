using Reloaded.Mod.Launcher.Lib.Remix.Updates;

namespace Reloaded.Mod.Launcher.Controls.Mods;

/// <summary>
/// Interaction logic for ModItem.xaml
/// </summary>
public partial class ModItem : UserControl
{
    public ModItem()
    {
        InitializeComponent();

        IconUpdate.PreviewMouseDown += (sender, arg) =>
        {
            if (DataContext is ModEntry entry)
            {
                if (entry.Status.Updates == UpdateStatus.Supported)
                {
                    Task.Run(UpdateService.CheckForUpdates);
                }
                else if (entry.Status.Updates == UpdateStatus.Pending)
                {
                    UpdateService.OpenUpdater();
                }
            }
        };

        IconConfig.PreviewMouseDown += (sender, arg) =>
        {
            if (DataContext is ModEntry entry)
            {
                if (entry.ConfigureModCommand.CanExecute(null))
                {
                    entry.ConfigureModCommand.Execute(null);
                }
            }
        };

        IconSupport.PreviewMouseDown += (sender, arg) =>
        {
            if (DataContext is ModEntry entry)
            {
                if (!string.IsNullOrEmpty(entry.Tuple.Config.CreatorUrl))
                {
                    ProcessExtensions.OpenHyperlink(entry.Tuple.Config.CreatorUrl);
                }
            }
        };
    }

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(ModItem),
            new PropertyMetadata(false)
        );

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }
}
