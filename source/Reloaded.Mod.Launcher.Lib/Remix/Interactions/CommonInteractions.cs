using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using System.Reactive;

namespace Reloaded.Mod.Launcher.Lib.Remix.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class CommonInteractions
{
    public static readonly Interaction<TextInputViewModel, string?> PromptTextInput = new();

    public static readonly Interaction<SelectFileConfig, string[]> SelectFile = new();

    public static readonly Interaction<SaveFileConfig, string?> SaveFile = new();

    public static readonly Interaction<SelectFolderConfig, string[]> SelectFolder = new();

    public static readonly Interaction<ToastConfig, Unit> Toast = new();
}
