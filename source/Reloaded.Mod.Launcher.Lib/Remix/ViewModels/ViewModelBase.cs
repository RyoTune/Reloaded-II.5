using ReactiveUI;
using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ViewModelBase : ObservableObject, IReactiveObject
{
    public ViewModelBase()
    {
        this.SubscribePropertyChangedEvents();
        this.SubscribePropertyChangingEvents();
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args) => this.OnPropertyChanged(args);

    public void RaisePropertyChanging(PropertyChangingEventArgs args) => this.OnPropertyChanging(args);
}

public class ReactiveViewModelBase : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
