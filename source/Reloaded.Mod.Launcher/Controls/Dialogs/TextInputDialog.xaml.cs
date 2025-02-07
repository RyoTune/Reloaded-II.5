using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using System.Reactive.Disposables;

namespace Reloaded.Mod.Launcher.Controls.Dialogs;

/// <summary>
/// Interaction logic for TextInputDialog.xaml
/// </summary>
public partial class TextInputDialog : ReactiveWindow<TextInputViewModel>
{
    public TextInputDialog()
    {
        InitializeComponent();

        this.WhenActivated((CompositeDisposable disp) =>
        {
            this.DataContext = this.ViewModel;

            RoutedEventHandler cancelHandler = (_, _) => { this.DialogResult = false; this.Close(); };
            this.Button_Cancel.Click += cancelHandler;

            RoutedEventHandler confirmHandler = (_, _) => { this.DialogResult = true; this.Close(); };
            this.Button_Confirm.Click += confirmHandler;

            Disposable.Create(() =>
            {
                this.Button_Cancel.Click -= cancelHandler;
                this.Button_Confirm.Click -= confirmHandler;
            })
            .DisposeWith(disp);
        });
    }
}
