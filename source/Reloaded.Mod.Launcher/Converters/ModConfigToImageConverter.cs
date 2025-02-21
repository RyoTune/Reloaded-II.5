namespace Reloaded.Mod.Launcher.Converters;

public class ModConfigToImageConverter : IMultiValueConverter, IValueConverter
{
    public static readonly ModConfigToImageConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        if (VeryImportantMemeUtils.TryRinFile(out var rinFile))
        {
            return Imaging.BitmapFromUri(new Uri(rinFile));
        }

        // value[0]: The path & config tuple.
        // value[1]: The icon path property. (config.Config.ModIcon). This is so we can receive property changed events.
        if (value[0] is PathTuple<ModConfig> config)
            return GetImageForModConfig(config);

        return null!;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            if (File.Exists(path))
            {
                return Imaging.BitmapFromUri(new Uri(path, UriKind.RelativeOrAbsolute));
            }
        }

        return Imaging.BitmapFromUri(WpfConstants.PlaceholderImagePath);
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtains an image to represent a given mod, either a custom one or the default placeholder.
    /// </summary>
    public ImageSource GetImageForModConfig(PathTuple<ModConfig> modConfig)
    {
        var uri = modConfig.Config.TryGetIconPath(modConfig.Path, out string iconPath) ? new Uri(iconPath, UriKind.RelativeOrAbsolute) : WpfConstants.PlaceholderImagePath;
        return Imaging.BitmapFromUri(uri);
    }
}