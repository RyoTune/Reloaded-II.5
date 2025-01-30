namespace Reloaded.Mod.Launcher.Converters;

public class ApplicationPathTupleToImageConverter : IMultiValueConverter
{
    public static ApplicationPathTupleToImageConverter Instance = new ApplicationPathTupleToImageConverter();

    private readonly static Dictionary<string, IconFile[]> appIcons = [];

    /// <inheritdoc />
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        // value[0]: The path & config tuple.
        // value[1]: The icon path property. (config.Config.AppIcon). This is so we can receive property changed events.
        if (value[0] is PathTuple<ApplicationConfig> config)
            return GetImageForAppConfig(config);

        return null!;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtains an image to represent a given application.
    /// The image is either a custom one or the icon of the application.
    /// </summary>
    private ImageSource GetImageForAppConfig(PathTuple<ApplicationConfig> appConfig)
    {
        // Check if custom icon exists.
        if (!string.IsNullOrEmpty(appConfig.Config.AppIcon))
        {
            if (ApplicationConfig.TryGetApplicationIcon(appConfig.Path, appConfig.Config, out var applicationIcon))
                return Misc.Imaging.BitmapFromUri(new Uri(applicationIcon, UriKind.Absolute));
        }

        // Otherwise extract new icon from executable.
        var appFile = ApplicationConfig.GetAbsoluteAppLocation(appConfig);
        if (File.Exists(appFile))
        {
            var iconFile = GetAppIcon(appFile, appConfig);
            if (iconFile != null)
            {
                var source = new BitmapImage(new(iconFile));
                source.Freeze();
                return source;
            }
        }

        return Misc.Imaging.GetPlaceholderIcon();
    }

    private static string? GetAppIcon(string appFile, PathTuple<ApplicationConfig> appConfig)
    {
        if (appIcons.TryGetValue(appConfig.Config.AppId, out var icons))
        {
            return icons.FirstOrDefault()?.File;
        }

        var iconsDir = GetAppIconsDir(appConfig);
        if (!Directory.EnumerateFiles(iconsDir, "*.png").Any())
        {
            DumpAppIcons(appFile, iconsDir);
        }

        var foundIcons = new List<IconFile>();
        foreach (var file in Directory.EnumerateFiles(iconsDir, "*.png"))
        {
            if (int.TryParse(Path.GetFileNameWithoutExtension(file), out var size))
            {
                foundIcons.Add(new(file, size));
            }
        }

        appIcons[appConfig.Config.AppId] = foundIcons.OrderByDescending(x => x.Size).ToArray();
        return appIcons[appConfig.Config.AppId].FirstOrDefault()?.File;
    }

    private static void DumpAppIcons(string appFile, string iconsDir)
    {
        var iconRes = IconInfo.LoadIconsFromBinary(appFile);
        foreach (var res in iconRes)
        {
            using var icon = Icon.FromHandle(res.Handle);
            using var bitmap = icon.ToBitmap();

            var outputFile = Path.Join(iconsDir, $"{icon.Width}.png");
            bitmap.Save(outputFile);
            res.Dispose();
        }
    }

    private static string GetAppIconsDir(PathTuple<ApplicationConfig> appConfig)
    {
        var appDir = Path.GetDirectoryName(appConfig.Path);
        var iconDir = Path.Join(appDir, "icons");
        Directory.CreateDirectory(iconDir);
        return iconDir;
    }

    private record IconFile(string File, int Size);
}