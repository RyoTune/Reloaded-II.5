using ColorThiefDotNet;
using Color = System.Windows.Media.Color;

namespace Reloaded.Mod.Launcher.Utility;

internal static class ApplicationIcon
{
    private readonly static Dictionary<string, IconFile[]> appIcons = [];
    private static readonly ColorThief _colorThief = new();

    public static Color GetColor(PathTuple<ApplicationConfig> appConfig)
    {
        var bitmap = GetBitmap(appConfig);
        var palette = _colorThief.GetPalette(bitmap);

        var color = palette.OrderByDescending(x => x.Population).First().Color;

        var rgbColor = new RgbColor() { Red = color.R, Green = color.G, Blue = color.B, Alpha = 255 };
        var hslColor = rgbColor.ToHslColor();
        var adjustedHsl = new HslColor(hslColor.Hue, 100, 85, byte.MaxValue);

        return adjustedHsl.ToColor();
    }

    public static Bitmap GetBitmap(PathTuple<ApplicationConfig>? appConfig)
    {
        var iconFile = GetAppIconFile(appConfig);
        if (iconFile != null)
        {
            return new(iconFile);
        }

        var placeholderFile = Path.Join(Directory.GetCurrentDirectory(), WpfConstants.PlaceholderImagePath.LocalPath);
        return new(placeholderFile);
    }

    /// <summary>
    /// Obtains an image to represent a given application.
    /// The image is either a custom one or the icon of the application.
    /// </summary>
    public static ImageSource GetImageSource(PathTuple<ApplicationConfig>? appConfig)
    {
        var iconFile = GetAppIconFile(appConfig);
        if (iconFile != null)
        {
            return Imaging.BitmapFromUri(new(iconFile));
        }

        return Imaging.GetPlaceholderIcon();
    }

    private static string? GetAppIconFile(PathTuple<ApplicationConfig>? appConfig)
    {
        if (appConfig == null) return null;

        // Check if custom icon exists.
        if (!string.IsNullOrEmpty(appConfig.Config.AppIcon))
        {
            if (ApplicationConfig.TryGetApplicationIcon(appConfig.Path, appConfig.Config, out var applicationIcon))
                return applicationIcon;
        }
        
        // Otherwise extract new icon from executable.
        var appFile = ApplicationConfig.GetAbsoluteAppLocation(appConfig);
        if (File.Exists(appFile))
        {
            var iconFile = GetAndDumpAppIcon(appFile, appConfig);
            if (iconFile != null)
            {
                return iconFile;
            }
        }

        return null;
    }

    private static string? GetAndDumpAppIcon(string appFile, PathTuple<ApplicationConfig> appConfig)
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
        var iconDir = Path.Join(Loader.IO.Paths.ConfigFolder, "Icons", appConfig.Config.AppId);
        Directory.CreateDirectory(iconDir);
        return iconDir;
    }

    private record IconFile(string File, int Size);
}
