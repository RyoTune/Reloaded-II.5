
namespace Reloaded.Mod.Launcher.Converters;

public class StringNullOrEmptyConverter : IValueConverter
{
    public static readonly StringNullOrEmptyConverter Instance = new StringNullOrEmptyConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str);
        }

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
