namespace Reloaded.Mod.Loader.IO.Remix.Apps;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AppVersions
{
    /// <summary>
    /// Gets list of application versions available for the given app config.
    /// App versions should follow the pattern of: NAME_VERSION.exe. Ex: Metaphor_1.0.3.exe
    /// </summary>
    /// <param name="appConfig">Application config to get versions for.</param>
    /// <returns></returns>
    public static AppVersion[] GetAvailableVersions(ApplicationConfig appConfig)
    {
        var appPath = Path.GetFullPath(appConfig.AppLocation);
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
        var fileVer = FileVersionInfo.GetVersionInfo(file)?.FileVersion;
        if (string.IsNullOrEmpty(fileVer) || fileVer == "0.0.0.0")
        {
            if (fileName.Length > fileName.Length + 1 && Version.TryParse(fileName[(fileName.Length + 1)..], out var version))
            {
                return new AppVersion(file, version);
            }

            return new AppVersion(file, new());
        }
        else
        {
            return new AppVersion(file, new Version(fileVer));
        }
    }

    private class AppVersionDistinctByVersion : IEqualityComparer<AppVersion>
    {
        public static readonly AppVersionDistinctByVersion Instance = new();

        public bool Equals(AppVersion x, AppVersion y) => x.Version == y.Version;

        public int GetHashCode([DisallowNull] AppVersion obj) => obj.Version.GetHashCode();
    }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public record AppVersion(string AppPath, Version Version);