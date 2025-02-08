using ReactiveUI;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;

namespace Reloaded.Mod.Launcher.Lib.Remix;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class Interactions
{
    public static readonly Interaction<TextInputViewModel, string?> PromptTextInput = new();
}
