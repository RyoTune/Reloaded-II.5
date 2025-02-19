namespace Reloaded.Mod.Launcher.Converters;

public class ApplicationPathTupleToImageConverter : IValueConverter, IMultiValueConverter
{
    public static readonly ApplicationPathTupleToImageConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Convert([value], targetType, parameter, culture);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is PathTuple<ApplicationConfig> config)
                return ApplicationIcon.GetImageSource(config);
        }

        return Imaging.BitmapFromUri(WpfConstants.PlaceholderImagePath);
    }

    /// <inheritdoc />
    public object? ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}