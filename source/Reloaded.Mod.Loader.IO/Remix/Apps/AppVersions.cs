namespace Reloaded.Mod.Loader.IO.Remix.Apps;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AppVersions
{
    /// <summary>
    /// Gets list of application versions available for the given app config.
    /// For apps that don't set the file version in the file, you can set add the version in the file's name.
    /// Example: <c>FILENAME_ver1.0.0.exe</c>, such as <c>Metaphor_ver1.0.3.exe</c>.
    /// </summary>
    /// <param name="appConfig">Application config to get versions for.</param>
    /// <returns></returns>
    public static AppVersion[] GetAvailableVersions(ApplicationConfig appConfig)
    {
        try
        {
            var appPath = Path.GetFullPath(appConfig.AppLocation);
            var appName = Path.GetFileNameWithoutExtension(appPath);

            var appVersions = Directory.EnumerateFiles(Path.GetDirectoryName(appPath)!, $"{appName}_*.exe")
                .Select(GetFileVersion)
                .ToList();

            appVersions.Insert(0, GetFileVersion(appPath));

            return appVersions.Distinct(AppVersionDistinctByVersion.Instance).OrderByDescending(x => x.Version).ToArray();
        }
        catch (Exception ex)
        {
            return Array.Empty<AppVersion>();
        }
    }

    public static AppVersion[] GetAvailableVersions(string appConfigPath)
    {
        var appPath = Path.GetFullPath(appConfigPath);
        var appName = Path.GetFileNameWithoutExtension(appPath);

        var appVersions = Directory.EnumerateFiles(Path.GetDirectoryName(appPath)!, $"{appName}_*.exe")
            .Select(GetFileVersion)
            .ToList();

        appVersions.Insert(0, GetFileVersion(appPath));

        return appVersions.Distinct(AppVersionDistinctByVersion.Instance).OrderByDescending(x => x.Version).ToArray();
    }

    public static AppVersion FindAppByVersion(string version, IEnumerable<AppVersion> appVersions)
        => appVersions.FirstOrDefault(x => x.Version.ToString() == version);

    private static AppVersion GetFileVersion(string file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var verStrIdx = fileName.IndexOf("_ver");
        if (verStrIdx != -1 && Version.TryParse(fileName[(verStrIdx + 2)..], out var nameVer))
        {
            return new AppVersion(file, nameVer);
        }

        const string unsetVer = "0.0.0";

        var fileVer = GetVersion(FileVersionInfo.GetVersionInfo(file));
        if (string.IsNullOrEmpty(fileVer) || fileVer == unsetVer)
        {
            return new AppVersion(file, new(unsetVer));
        }
        else
        {
            return new AppVersion(file, new Version(fileVer));
        }
    }

    private static string GetVersion(FileVersionInfo info)
        => (info.FilePrivatePart == 0) ? $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}" : $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}.{info.FilePrivatePart}";

    private class AppVersionDistinctByVersion : IEqualityComparer<AppVersion>
    {
        public static readonly AppVersionDistinctByVersion Instance = new();

        public bool Equals(AppVersion x, AppVersion y) => x.Version == y.Version;

        public int GetHashCode([DisallowNull] AppVersion obj) => obj.Version.GetHashCode();
    }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public record AppVersion(string AppPath, Version Version);