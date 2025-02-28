using ReactiveUI;

namespace Reloaded.Mod.Launcher.Lib.Remix.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class IReactiveObjectExtensions
{
    /// <summary>
    /// ReactiveObject equivalent of CommunityToolkit's SetProperty wrapping the properties of non-notifying model.
    /// </summary>
    public static T RaiseAndSetIfChanged<TObj, TModel, T>(
        this TObj reactiveObject,
        T oldValue,
        T newValue,
        TModel model,
        Action<TModel, T> callback,
        [CallerMemberName] string? propertyName = null)
        where TObj : IReactiveObject
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(callback);
            ArgumentNullException.ThrowIfNull(propertyName);

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return newValue;
            }

            reactiveObject.RaisePropertyChanging(propertyName!);
            callback(model, newValue);
            reactiveObject.RaisePropertyChanged(propertyName!);
            return newValue;
        }
}
