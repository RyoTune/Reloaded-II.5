namespace Reloaded.Mod.Launcher.Controls.Mods;

/// <summary>
/// Interaction logic for ModItem.xaml
/// </summary>
public partial class ModItem : UserControl
{
    public ModItem()
    {
        InitializeComponent();
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
