namespace Reloaded.Mod.Launcher.Converters;

public class ApplicationPathTupleToImageConverter : IValueConverter
{
    public static readonly ApplicationPathTupleToImageConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PathTuple<ApplicationConfig> config)
            return ApplicationIcon.GetImageSource(config);

        return Imaging.BitmapFromUri(WpfConstants.PlaceholderImagePath);
    }

    /// <inheritdoc />
    public object? ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}