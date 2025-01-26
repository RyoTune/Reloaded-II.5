namespace Reloaded.Mod.Launcher.Controls;

public class DropdownMenuControl : ContentControl
{
    private Popup? _popup;

    static DropdownMenuControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DropdownMenuControl), new FrameworkPropertyMetadata(typeof(DropdownMenuControl)));
    }

    public override void OnApplyTemplate()
    {
        _popup = Template.FindName("Dropdown_Popup", this) as Popup;
        if (_popup != null)
        {
            _popup.Closed += Popup_Closed;
        }

        base.OnApplyTemplate();
    }

    private void Popup_Closed(object? sender, EventArgs e)
    {
        IsOpen = false;
    }

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(string), typeof(DropdownMenuControl), new PropertyMetadata(string.Empty));

    public bool IsOpen
    {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register("IsOpen", typeof(bool), typeof(DropdownMenuControl), new PropertyMetadata(false));
}
