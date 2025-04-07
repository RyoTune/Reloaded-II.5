using FileMode = System.IO.FileMode;

namespace Reloaded.Mod.Launcher.Lib.Remix.Utils;

internal static class AsiLoader
{
    public static bool TryDeployAsi(string appPath, [NotNullWhen(true)] out string? loaderPath, [NotNullWhen(true)] out string? bootstrapperPath)
    {
        loaderPath = null;
        bootstrapperPath = null;

        if (!File.Exists(appPath)) return false;

        if (CanDeploy(appPath))
        {
            DeployAsiLoader(appPath, out loaderPath, out bootstrapperPath);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryRemoveAsi(string appPath, out string? loaderPath, out string? bootstrapperPath)
    {
        loaderPath = null;
        bootstrapperPath = null;

        if (!File.Exists(appPath)) return false;

        if (!CanDeploy(appPath))
        {
            return true;
        }

        loaderPath = GetAsiLoader(appPath);
        bootstrapperPath = GetBootstrapperInstallPath(appPath, out _);

        try
        {
            if (File.Exists(loaderPath))
            {
                File.Delete(loaderPath);
            }

            if (File.Exists(bootstrapperPath))
            {
                File.Delete(bootstrapperPath);
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private static bool Is64Bit(string filePath)
    {
        using var parser = new BasicPeParser(filePath);
        return !parser.Is32BitHeader;
    }

    /// <summary>
    /// Checks if the ASI loader can be deployed.
    /// </summary>
    public static bool CanDeploy(string appPath)
    {
        try
        {
            using var peParser = new BasicPeParser(appPath);
            return GetSupportedDllFromParser(peParser);
        }
        catch (Exception e)
        {
            Errors.HandleException(e, Resources.ErrorCantReadExeFileAsiLoaderDeploy.Get());
            return false;
        }
    }

    private static bool GetSupportedDllFromParser(BasicPeParser peParser)
    {
        try
        {
            return GetFirstSupportedDllFile(peParser) != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Deploys the ASI loader (if needed) and bootstrapper to the game folder.
    /// </summary>
    public static void DeployAsiLoader(string appPath, out string asiLoaderPath, out string bootstrapperPath)
    {
        asiLoaderPath = GetAsiLoader(appPath);
        DeployBootstrapper(appPath, out bool alreadyHasAsiPlugins, out bootstrapperPath);
        if (alreadyHasAsiPlugins)
            return;

        using var peParser = new BasicPeParser(appPath);
        ExtractAsiLoader(asiLoaderPath, !peParser.Is32BitHeader);
    }

    private static string GetAsiLoader(string appPath)
    {
        using var peParser = new BasicPeParser(appPath);
        var appDirectory = Path.GetDirectoryName(appPath);
        var dllName = GetFirstSupportedDllFile(peParser);
        return Path.Combine(appDirectory!, dllName!);
    }

    /// <summary>
    /// Gets the path to which the bootstrapper will be copied to should it be installed.
    /// </summary>
    /// <param name="appPath">Application path.</param>
    /// <param name="alreadyHasAsiPlugins">True if at least 1 ASI plugin is already installed.</param>
    /// <returns>The path to which the bootstrapper will be copied to.</returns>
    public static string GetBootstrapperInstallPath(string appPath, out bool alreadyHasAsiPlugins)
    {
        var installFolder = GetBootstrapperInstallFolder(appPath, out alreadyHasAsiPlugins);
        var bootstrapperPath = GetBootstrapperDllPath(appPath);
        return Path.Combine(installFolder, Path.ChangeExtension(Path.GetFileName(bootstrapperPath), PluginExtension));
    }

    /// <summary>
    /// Gets the path of the bootstrapper DLL to copy.
    /// </summary>
    public static string GetBootstrapperDllPath(string appPath)
    {
        return Is64Bit(appPath)
            ? IoC.Get<LoaderConfig>().Bootstrapper64Path
            : IoC.Get<LoaderConfig>().Bootstrapper32Path;
    }

    /// <summary>
    /// Returns true if ASI loader is already installed, else false.
    /// This check works by checking the existence of ASI files in a supported directory.
    /// </summary>
    private static bool AreAnyAsiPluginsInstalled(string appPath, out string? modPath)
    {
        var appDirectory = Path.GetDirectoryName(appPath);
        foreach (var directory in AsiCommonDirectories)
        {
            var directoryPath = Path.Combine(appDirectory!, directory);

            if (!Directory.Exists(directoryPath))
                continue;

            if (!Directory.GetFiles(directoryPath).Any(x => x.EndsWith(PluginExtension, StringComparison.OrdinalIgnoreCase)))
                continue;

            modPath = directoryPath;
            return true;
        }

        modPath = null;
        return false;
    }

    /// <summary>
    /// Gets the folder to install the bootstrapper to.
    /// Returned folder should be created if it did not previously exist.
    /// </summary>
    /// <param name="appPath">Application path.</param>
    /// <param name="alreadyHasAsiPlugins">Whether a supported folder with at least one ASI Plugin already exists. Assume loader already installed if it does.</param>
    private static string GetBootstrapperInstallFolder(string appPath, out bool alreadyHasAsiPlugins)
    {
        alreadyHasAsiPlugins = false;

        if (AreAnyAsiPluginsInstalled(appPath, out string? installPath))
        {
            alreadyHasAsiPlugins = true;
            return installPath!;
        }

        var appDirectory = Path.GetDirectoryName(appPath);
        var pluginDirectory = Path.Combine(appDirectory!, AsiCommonDirectories[0]);
        return pluginDirectory;
    }

    private static void DeployBootstrapper(string appPath, out bool alreadyHasAsiPlugins, out string bootstrapperInstallPath)
    {
        bootstrapperInstallPath = GetBootstrapperInstallPath(appPath, out alreadyHasAsiPlugins);
        var bootstrapperDir = Path.GetDirectoryName(bootstrapperInstallPath);

        if (!Directory.Exists(bootstrapperDir))
        {
            try
            {
                Directory.CreateDirectory(bootstrapperDir!);
            }
            catch (Exception e)
            {
                throw new IOException(string.Format(Resources.BootstrapperCreateDirectoryError.Get(), e.Message));
            }
        }

        File.Copy(GetBootstrapperDllPath(appPath), bootstrapperInstallPath, true);
    }

    /// <summary>
    /// Get name of first DLL file using which ASI loader can be installed.
    /// </summary>
    /// <param name="peParser">Parsed PE file.</param>
    private static string? GetFirstSupportedDllFile(BasicPeParser peParser)
    {
        string? GetSupportedDll(BasicPeParser file, string[] supportedDlls)
        {
            var names = file.GetImportDescriptorNames();
            return names.FirstOrDefault(x => supportedDlls.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        return GetSupportedDll(peParser, peParser.Is32BitHeader ? AsiLoaderSupportedDll32 : AsiLoaderSupportedDll64);
    }

    /// <summary>
    /// Extracts the ASI loader to a given path.
    /// </summary>
    /// <param name="filePath">Absolute file path to extract loader to.</param>
    /// <param name="is64bit">Whether loader is 64 bit or not.</param>
    private static void ExtractAsiLoader(string filePath, bool is64bit)
    {
        var libraryDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        var compressedLoaderPath = $"{libraryDirectory}/Loader/Asi/UltimateAsiLoader.7z";

        var archive = new SevenZipExtractor(compressedLoaderPath);
        using var writeStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
        archive.ExtractFile(is64bit ? "ASILoader64.dll" : "ASILoader32.dll", writeStream);
    }

    #region Constants
    private static string PluginExtension = ".asi";

    private static readonly string[] AsiLoaderSupportedDll32 =
    {
        "xlive.dll",
        "winmm.dll",
        "wininet.dll",
        "vorbisFile.dll",
        "version.dll",
        "msvfw32.dll",
        "msacm32.dll",
        "dsound.dll",
        "dinput8.dll",
        "dinput.dll",
        "ddraw.dll",
        "d3d11.dll",
        "d3d9.dll",
        "d3d8.dll"
    };

    private static readonly string[] AsiLoaderSupportedDll64 =
    {
        "winmm.dll",
        "wininet.dll",
        "dsound.dll",
        "dinput8.dll",
        "version.dll"
    };

    private static readonly string[] AsiCommonDirectories =
    {
        "", // root folder
        "scripts",
        "plugins"
    };
    #endregion
}
