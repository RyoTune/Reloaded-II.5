namespace Reloaded.Mod.Launcher.Lib.Remix.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ToastConfig
{
    public string Message { get; set; } = string.Empty;

    public bool ShowDateTime { get; set; }

    public int Duration { get; set; } = 6;

    public string? CancelText { get; set; }

    public string? ConfirmText { get; set; }

    public bool StaysOpen { get; set; }

    public string? Token { get; set; }

    public ToastType Type { get; set; } = ToastType.Info;

    public Func<bool, bool>? PromptFunc { get; set; }

    public enum ToastType
    {
        Info,
        Warning,
        Error,
        Success,

        Prompt,
    }
}
