namespace Reloaded.Mod.Launcher.Controls;

public class LinkControl : ContentControl
{
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        e.Handled = true;

        try
        {
            if (string.IsNullOrEmpty(Path)) return;

            var procInfo = new ProcessStartInfo() { FileName = Path, UseShellExecute = true };
            Process.Start(procInfo);
        }
        catch (Exception) { }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        this.ToolTip ??= Path;
    }

    static LinkControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LinkControl), new FrameworkPropertyMetadata(typeof(LinkControl)));
    }

    public string? Path
    {
        get { return (string?)GetValue(PathProperty); }
        set { SetValue(PathProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PathProperty =
        DependencyProperty.Register("Path", typeof(string), typeof(LinkControl), new PropertyMetadata(null));
}
