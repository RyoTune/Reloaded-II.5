namespace Reloaded.Mod.Loader;

public static class Errors
{
    public const string UnableToFindApplication = "Unable to find an app configuration for the currently executing EXE.\n" +
                                                  "Please go to `Edit Application` in Reloaded launcher and click `Update`.\n" +
                                                  "Usually this can happen because:\n" +
                                                  "- You moved your application folder.\n" + 
                                                  "- App folder was moved after an update (can happen with GamePass/UWP).";
    public const string ModLoaderNotInitialized = "Mod loader has not been initialized.";
    public const string ModLoaderAlreadyInitialized = "Mod loader is already initialized.";

    public static string ModToLoadNotFound(string modId)        => $"[Load] Mod with ID ({modId}) not found.";
    public static string ModToUnloadNotFound(string modId)      => $"[Unload] Mod with ID ({modId}) not found.";
    public static string ModToSuspendNotFound(string modId)     => $"[Suspend] Mod with ID ({modId}) not found.";
    public static string ModToResumeNotFound(string modId)      => $"[Resume] Mod with ID ({modId}) not found.";

    public static string ModSuspendNotSupported(string modId)   => $"Suspend/Resume is not supported by this mod. ({modId})";
    public static string ModUnloadNotSupported(string modId)    => $"Load/Unload is not supported by this mod. ({modId})";

    public static string ModAlreadyLoaded(string modId)         => $"Mod with specified ID ({modId}) is already loaded.";
}